import os
import time
import uuid
import json
import re
import logging
import torch
import signal
import sys
from faster_whisper import WhisperModel
import redis
import hashlib

temp_dir = os.environ.get('TEMP_FILE_DIR', '/app/temp_files')
os.makedirs(temp_dir, exist_ok=True)

# Cấu hình logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger("whisper-worker")

# Cấu hình worker từ biến môi trường
REDIS_HOST = os.environ.get('REDIS_HOST', 'localhost')
REDIS_PORT = int(os.environ.get('REDIS_PORT', 6379))
MODEL_SIZE = os.environ.get('MODEL_SIZE', 'small')
DEVICE = os.environ.get('DEVICE', 'cpu')
COMPUTE_TYPE = os.environ.get('COMPUTE_TYPE', 'int8')
NUM_THREADS = int(os.environ.get('NUM_THREADS', 6))
WORKER_ID = str(uuid.uuid4())[:8]

# Tối ưu PyTorch cho CPU
torch.set_num_threads(NUM_THREADS)
torch.set_num_interop_threads(1)  # Giảm thread contention

# Cấu hình OpenMP
os.environ['OMP_NUM_THREADS'] = str(NUM_THREADS)
os.environ['MKL_NUM_THREADS'] = str(NUM_THREADS)
os.environ['NUMEXPR_NUM_THREADS'] = str(NUM_THREADS)
os.environ['OMP_SCHEDULE'] = 'static'
os.environ['OMP_PROC_BIND'] = 'close'

# Kết nối Redis
redis_client = redis.Redis(host=REDIS_HOST, port=REDIS_PORT)

# Khởi tạo model
def load_model():
    logger.info(f"Loading Whisper model (size: {MODEL_SIZE}, device: {DEVICE}, compute_type: {COMPUTE_TYPE}, threads: {NUM_THREADS})")
    return WhisperModel(
        MODEL_SIZE, 
        device=DEVICE, 
        compute_type=COMPUTE_TYPE,
        cpu_threads=NUM_THREADS,
        num_workers=2  # Worker cho data loading
    )

# Từ điển chuyển đổi từ chữ sang số
number_map = {
    # Tiếng Việt
    "không": "0", "một": "1", "hai": "2", "ba": "3", "bốn": "4", "năm": "5",
    "sáu": "6", "bảy": "7", "bẩy": "7", "bày": "7", "tám": "8", "tắm": "8", "chín": "9",
    # Tiếng Anh
    "zero": "0", "one": "1", "two": "2", "three": "3", "four": "4", "five": "5",
    "six": "6", "seven": "7", "eight": "8", "nine": "9"
}

# Chuyển đổi text số
def text_to_number_with_others(text):
    # Chuyển về chữ thường để xử lý thống nhất
    text = text.lower()
    
    # Sử dụng regex để thay thế từng từ số riêng lẻ
    pattern = r'\b(' + '|'.join(number_map.keys()) + r')\b'
    text = re.sub(pattern, lambda x: number_map[x.group()], text)

    return text.replace("-", "").replace(",", "")

# Tiền xử lý audio để tối ưu hiệu suất
def preprocess_audio(file_path):
    import subprocess
    
    # Tạo file tạm thời đã tối ưu
    output_path = f"{file_path}.optimized.wav"
    
    try:
        # Chuyển đổi sang 16kHz, mono, 16-bit PCM
        subprocess.run([
            "ffmpeg", "-i", file_path, 
            "-ar", "16000", "-ac", "1", "-c:a", "pcm_s16le",
            "-y", output_path
        ], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        
        return output_path
    except Exception as e:
        logger.warning(f"Failed to preprocess audio: {e}, using original file")
        return file_path

# Cache kết quả transcription
def get_cached_result(file_path):
    try:
        # Tạo hash từ nội dung file
        with open(file_path, "rb") as f:
            file_hash = hashlib.md5(f.read()).hexdigest()
        
        # Kiểm tra cache
        cached = redis_client.get(f"transcription_cache:{file_hash}")
        if cached:
            return json.loads(cached.decode('utf-8'))
    except Exception as e:
        logger.warning(f"Failed to check cache: {e}")
    
    return None

def save_to_cache(file_path, result):
    try:
        # Tạo hash từ nội dung file
        with open(file_path, "rb") as f:
            file_hash = hashlib.md5(f.read()).hexdigest()
        
        # Lưu vào cache với TTL 1 giờ
        redis_client.setex(
            f"transcription_cache:{file_hash}", 
            3600,  # 1 hour TTL
            json.dumps(result)
        )
    except Exception as e:
        logger.warning(f"Failed to save to cache: {e}")

# Xử lý transcription
def process_task(task_id):
    try:
        # Get task data
        task_json = redis_client.get(f"task:{task_id}")
        if not task_json:
            logger.error(f"Task {task_id} not found")
            return
            
        task_data = json.loads(task_json)
        
        file_path = task_data["file_path"]
        filename = task_data["filename"]
        
        logger.info(f"Processing task {task_id} for file {filename}")
        logger.info(f"File path: {file_path}")
        
        # Kiểm tra file tồn tại
        if not os.path.exists(file_path):
            logger.error(f"File not found: {file_path}")
            
            # Thử tìm file trong thư mục tạm thời
            alt_path = os.path.join(temp_dir, os.path.basename(file_path))
            if os.path.exists(alt_path):
                logger.info(f"Found file at alternative path: {alt_path}")
                file_path = alt_path
                # Cập nhật đường dẫn trong task_data
                task_data["file_path"] = file_path
                redis_client.set(f"task:{task_id}", json.dumps(task_data))
            else:
                # Không tìm thấy file, cập nhật status và return
                task_data["status"] = "failed"
                task_data["error"] = f"File not found: {file_path}"
                redis_client.set(f"task:{task_id}", json.dumps(task_data))
                redis_client.incr("stats:failed_tasks")
                return
        
        # Update status
        task_data["status"] = "processing"
        task_data["worker_id"] = WORKER_ID
        task_data["started_at"] = time.time()
        redis_client.set(f"task:{task_id}", json.dumps(task_data))
        
        # Kiểm tra cache trước khi xử lý
        cached_result = get_cached_result(file_path)
        if cached_result:
            logger.info(f"Cache hit for task {task_id}")
            
            # Update task with cached results
            task_data["status"] = "completed"
            task_data["original_text"] = cached_result["original_text"]
            task_data["processed_text"] = cached_result["processed_text"]
            task_data["processing_time"] = cached_result["processing_time"]
            task_data["completed_at"] = time.time()
            task_data["from_cache"] = True
            
            redis_client.set(f"task:{task_id}", json.dumps(task_data))
            redis_client.incr("stats:processed_tasks")
            
            logger.info(f"Task {task_id} completed from cache")
            return
        
        # Tiền xử lý audio
        optimized_file = preprocess_audio(file_path)
        
        # Transcribe audio
        start_time = time.time()
        
        # Tối ưu transcription cho tiếng Việt
        segments, _ = model.transcribe(
            optimized_file,
            beam_size=5,
            language="vi",
            temperature=0.0,
            word_timestamps=False,
            vad_filter=True,
            vad_parameters={"threshold": 0.5}
        )
        
        text = " ".join([segment.text for segment in segments])
        
        # Process numbers
        processed_text = text_to_number_with_others(text)
        
        end_time = time.time()
        processing_time = f"{end_time - start_time:.2f} seconds"
        
        # Tạo kết quả
        result = {
            "original_text": text,
            "processed_text": processed_text,
            "processing_time": processing_time
        }
        
        # Lưu vào cache
        save_to_cache(file_path, result)
        
        # Update task with results
        task_data["status"] = "completed"
        task_data["original_text"] = text
        task_data["processed_text"] = processed_text
        task_data["processing_time"] = processing_time
        task_data["completed_at"] = time.time()
        
        redis_client.set(f"task:{task_id}", json.dumps(task_data))
        redis_client.incr("stats:processed_tasks")
        
        logger.info(f"Task {task_id} completed in {processing_time}")
        
        # Remove temporary files
        try:
            if optimized_file != file_path and os.path.exists(optimized_file):
                os.remove(optimized_file)
            if os.path.exists(file_path):
                os.remove(file_path)
        except Exception as e:
            logger.warning(f"Could not remove temp files: {str(e)}")
            
    except Exception as e:
        logger.error(f"Error processing task {task_id}: {str(e)}")
        
        # Update task with error
        try:
            task_data["status"] = "failed"
            task_data["error"] = str(e)
            task_data["completed_at"] = time.time()
            redis_client.set(f"task:{task_id}", json.dumps(task_data))
            redis_client.incr("stats:failed_tasks")
        except:
            pass

# Worker heartbeat
def send_heartbeat():
    worker_data = {
        "worker_id": WORKER_ID,
        "model_size": MODEL_SIZE,
        "device": DEVICE,
        "compute_type": COMPUTE_TYPE,
        "num_threads": NUM_THREADS,
        "last_heartbeat": time.time()
    }
    redis_client.set(f"worker:{WORKER_ID}", json.dumps(worker_data), ex=120)  # expires in 2 minutes

# Graceful shutdown
def handle_shutdown(sig, frame):
    logger.info(f"Worker {WORKER_ID} shutting down gracefully...")
    sys.exit(0)

# Main worker loop
def worker_loop():
    signal.signal(signal.SIGTERM, handle_shutdown)
    signal.signal(signal.SIGINT, handle_shutdown)
    
    logger.info(f"Worker {WORKER_ID} started")
    
    while True:
        try:
            # Send heartbeat
            send_heartbeat()
            
            # Get task from queue (blocking)
            task_data = redis_client.brpop("transcription_queue", timeout=1)
            
            if task_data:
                _, task_id = task_data
                task_id = task_id.decode('utf-8')
                process_task(task_id)
            else:
                # No task available, wait a bit
                time.sleep(0.5)
                
        except Exception as e:
            logger.error(f"Error in worker loop: {str(e)}")
            time.sleep(5)  # Wait before retry

# Startup
if __name__ == "__main__":
    # Load model
    model = load_model()
    
    # Start worker loop
    worker_loop()