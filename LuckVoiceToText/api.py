import os
import time
import uuid
import json
from fastapi import FastAPI, UploadFile, File, Depends, HTTPException, Security, status
from fastapi.security.api_key import APIKeyHeader, APIKey
from fastapi.middleware.cors import CORSMiddleware
import redis
import logging

# Cấu hình logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger("luckai-api")

# Khởi tạo Redis
redis_host = os.environ.get('REDIS_HOST', 'localhost')
redis_port = int(os.environ.get('REDIS_PORT', 6379))
redis_client = redis.Redis(host=redis_host, port=redis_port)

# API Key authentication
API_KEYS = os.environ.get('API_KEYS', 'test_key').split(',')
API_KEY_HEADER = APIKeyHeader(name="X-API-Key", auto_error=False)

# Khởi tạo FastAPI
app = FastAPI(
    title="Whisper Transcription API",
    description="API for audio transcription with Whisper",
    version="1.0.0"
)

# CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# API Key validation


async def get_api_key(api_key_header: str = Security(API_KEY_HEADER)):
    if api_key_header is None:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="API Key header is missing"
        )
    if api_key_header not in API_KEYS:
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Invalid API Key"
        )
    return api_key_header

# API routes


@app.post("/transcribe/", status_code=status.HTTP_202_ACCEPTED)
async def transcribe_audio_api(file: UploadFile = File(...)):
    """
    Submit an audio file for transcription. 
    Returns a task_id that can be used to check the result.
    """
    try:
        # Read file
        file_content = await file.read()

        # Generate unique task ID
        task_id = str(uuid.uuid4())

        # Save file temporarily
        temp_dir = os.environ.get('TEMP_FILE_DIR', '/app/temp_files')
        os.makedirs(temp_dir, exist_ok=True)  # Đảm bảo thư mục tồn tại
        temp_path = f"{temp_dir}/{task_id}_{file.filename}"

        # Ghi file với quyền phù hợp
        with open(temp_path, "wb") as f:
            f.write(file_content)
        os.chmod(temp_path, 0o666)  # Phân quyền 666 để đảm bảo có thể đọc/ghi

        # Add task to Redis queue
        task_data = {
            "task_id": task_id,
            "filename": file.filename,
            "file_path": temp_path,
            "status": "pending",
            "created_at": time.time()
        }

        # Store task info
        redis_client.set(f"task:{task_id}", json.dumps(task_data))

        # Add to processing queue
        redis_client.lpush("transcription_queue", task_id)

        logger.info(f"Task {task_id} added to queue for file {file.filename}")

        return {
            "task_id": task_id,
            "status": "pending",
            "message": "Your audio is queued for processing"
        }

    except Exception as e:
        logger.error(f"Error submitting task: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error submitting transcription task: {str(e)}"
        )


@app.get("/transcribe/{task_id}")
async def get_transcription_result(task_id: str):
    """
    Get the result of a transcription task.
    """
    try:
        # Check if task exists
        task_json = redis_client.get(f"task:{task_id}")

        if not task_json:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail="Task not found"
            )

        task_data = json.loads(task_json)

        # Return result based on status
        if task_data["status"] == "completed":
            return {
                "task_id": task_id,
                "status": "completed",
                "original_text": task_data.get("original_text", ""),
                "processed_text": task_data.get("processed_text", ""),
                "processing_time": task_data.get("processing_time", "")
            }
        elif task_data["status"] == "failed":
            return {
                "task_id": task_id,
                "status": "failed",
                "error": task_data.get("error", "Unknown error")
            }
        else:
            # Calculate position in queue
            queue_position = 0
            queue_items = redis_client.lrange("transcription_queue", 0, -1)
            for item in queue_items:
                if item.decode() == task_id:
                    break
                queue_position += 1

            return {
                "task_id": task_id,
                "status": "pending",
                "queue_position": queue_position,
                "message": "Your audio is still being processed"
            }

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error retrieving task {task_id}: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error retrieving transcription task: {str(e)}"
        )


@app.get("/health")
async def health_check():
    """
    Check API health and get queue statistics.
    """
    try:
        # Basic Redis health check
        redis_client.ping()

        # Get queue stats
        queue_length = redis_client.llen("transcription_queue")
        processed_tasks = int(redis_client.get("stats:processed_tasks") or 0)
        failed_tasks = int(redis_client.get("stats:failed_tasks") or 0)

        # Get active workers
        active_workers = []
        worker_keys = redis_client.keys("worker:*")
        for key in worker_keys:
            worker_data = json.loads(redis_client.get(key))
            # Active in last 60 seconds
            if time.time() - worker_data["last_heartbeat"] < 60:
                active_workers.append(worker_data)

        return {
            "status": "healthy",
            "queue": {
                "pending_tasks": queue_length,
                "processed_tasks": processed_tasks,
                "failed_tasks": failed_tasks
            },
            "active_workers": len(active_workers),
            "worker_details": active_workers
        }
    except Exception as e:
        logger.error(f"Health check failed: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail=f"Service unhealthy: {str(e)}"
        )
