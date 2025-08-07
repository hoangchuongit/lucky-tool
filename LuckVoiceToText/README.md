# LuckVoiceToText - Hệ thống Voice-to-Text với Docker

Hệ thống Voice-to-Text sử dụng Whisper AI với kiến trúc microservices, hỗ trợ xử lý audio thành text một cách hiệu quả và có thể mở rộng.

## 🏗️ Kiến trúc hệ thống

- **API Gateway**: FastAPI - Nhận và phân phối request
- **Worker**: Xử lý transcription với Faster-Whisper
- **Redis**: Quản lý queue và cache
- **Docker**: Container hóa và orchestration

## 📋 Yêu cầu hệ thống

### Tối thiểu
- Docker & Docker Compose
- 4GB RAM
- 4 CPU cores
- 10GB storage

### Khuyến nghị
- 16GB RAM
- 10 CPU cores
- 50GB storage
- SSD để tăng tốc I/O

## 🚀 Cài đặt và khởi chạy

### 1. Clone repository

```bash
git clone <repository-url>
cd LuckVoiceToText
```

### 2. Cấu hình biến môi trường

Tạo file `.env` trong thư mục gốc:

```bash
# API Configuration
API_KEYS=your_secret_key1,your_secret_key2,your_secret_key3

# Whisper Model Configuration
MODEL_SIZE=small  # tiny, base, small, medium, large-v1, large-v2, large-v3
DEVICE=cpu        # cpu hoặc cuda (nếu có GPU)
COMPUTE_TYPE=int8 # float16, int8, int8_float16

# Performance Tuning
NUM_THREADS=6
MAX_CONCURRENT_PER_WORKER=3
```

**Lưu ý về MODEL_SIZE:**
- `tiny`: Nhanh nhất, độ chính xác thấp nhất (~39 MB)
- `base`: Cân bằng tốt (~74 MB)
- `small`: Khuyến nghị cho production (~244 MB)
- `medium`: Độ chính xác cao (~769 MB)
- `large-v3`: Độ chính xác cao nhất (~1550 MB)

### 3. Khởi chạy với Docker Compose

```bash
# Build và khởi chạy tất cả services
docker-compose up -d

# Xem logs
docker-compose logs -f

# Kiểm tra trạng thái
docker-compose ps
```

### 4. Sử dụng script tự động (Ubuntu/Linux)

```bash
# Cấp quyền thực thi
chmod +x scripts/setup.sh scripts/start.sh

# Chạy script cài đặt (cần sudo)
sudo ./scripts/setup.sh

# Hoặc chạy script khởi động
./scripts/start.sh
```

## 📡 API Endpoints

### Base URL
```
http://localhost:8083
```

### 1. Health Check
```bash
curl http://localhost:8083/health
```

### 2. Transcribe Audio
```bash
curl -X POST http://localhost:8083/transcribe \
  -H "X-API-Key: your_secret_key1" \
  -F "file=@audio.mp3" \
  -F "language=vi"
```

**Supported formats:** mp3, wav, m4a, aac, ogg, flac, wma

**Response:**
```json
{
  "task_id": "12345678-1234-1234-1234-123456789abc",
  "status": "processing"
}
```

### 3. Get Result
```bash
curl http://localhost:8083/result/12345678-1234-1234-1234-123456789abc \
  -H "X-API-Key: your_secret_key1"
```

**Response:**
```json
{
  "task_id": "12345678-1234-1234-1234-123456789abc",
  "status": "completed",
  "result": {
    "text": "Nội dung được transcribe...",
    "language": "vi",
    "segments": [...],
    "processing_time": 2.5
  }
}
```

## 🔧 Cấu hình nâng cao

### Scaling Workers

Để tăng số lượng worker xử lý:

```yaml
# Trong docker-compose.yml
worker:
  deploy:
    replicas: 5  # Tăng từ 3 lên 5
```

### Resource Limits

Điều chỉnh resource limits trong `docker-compose.yml`:

```yaml
deploy:
  resources:
    limits:
      cpus: '2.5'
      memory: 4G
    reservations:
      cpus: '1.0'
      memory: 2G
```

### Model Caching

Model sẽ được cache tự động trong volume `whisper_cache`. Lần đầu download có thể mất thời gian:
- tiny: ~1 phút
- small: ~3 phút  
- medium: ~5 phút
- large: ~10 phút

## 📊 Monitoring

### Kiểm tra logs

```bash
# Tất cả services
docker-compose logs -f

# Chỉ API
docker-compose logs -f api

# Chỉ Worker
docker-compose logs -f worker

# Chỉ Redis
docker-compose logs -f redis
```

### Kiểm tra Redis queue

```bash
# Vào Redis container
docker-compose exec redis redis-cli

# Kiểm tra queue
> LLEN task_queue
> LLEN result_queue
```

### Kiểm tra tài nguyên

```bash
# CPU và Memory usage
docker stats

# Disk usage
docker system df
```

## 🐛 Troubleshooting

### 1. Worker không start được

**Lỗi:** `OMP: Error #15: Initializing libiomp5.dylib, but found libomp.dylib already initialized`

**Giải pháp:** Thêm vào Dockerfile.worker:
```dockerfile
ENV KMP_DUPLICATE_LIB_OK=TRUE
```

### 2. Model download chậm

**Lỗi:** Model download bị timeout

**Giải pháp:**
```bash
# Download trước model vào cache
docker-compose run --rm worker python -c "
from faster_whisper import WhisperModel
model = WhisperModel('small')
print('Model downloaded successfully')
"
```

### 3. Out of Memory

**Lỗi:** Worker bị kill do hết RAM

**Giải pháp:**
- Giảm `MODEL_SIZE` xuống `tiny` hoặc `base`
- Giảm `replicas` của worker
- Tăng RAM cho system
- Sử dụng `COMPUTE_TYPE=int8`

### 4. API không accessible

**Kiểm tra:**
```bash
# Port có đang listen không
netstat -tlnp | grep 8083

# Firewall rules
sudo ufw status

# Container networking
docker network ls
docker-compose exec api ping redis
```

## 🔒 Security

### API Keys

- Thay đổi `API_KEYS` trong `.env`
- Sử dụng key phức tạp, ít nhất 32 ký tự
- Rotate keys định kỳ
- Monitor API usage

### Network Security

```bash
# Chỉ expose cần thiết
# Trong docker-compose.yml, comment out ports nếu chỉ internal access
# ports:
#   - "8083:8000"
```

### File Permissions

```bash
# Đảm bảo temp files được clean up
docker-compose exec api ls -la /app/temp_files
docker-compose exec worker ls -la /app/temp_files
```

## 📈 Performance Tips

1. **SSD Storage**: Sử dụng SSD cho Docker volumes
2. **RAM**: Ít nhất 4GB RAM cho mỗi worker instance
3. **CPU**: Điều chỉnh `NUM_THREADS` theo số CPU cores
4. **Model Size**: Cân bằng giữa tốc độ và độ chính xác
5. **Batch Processing**: Xử lý multiple files cùng lúc

## 🛑 Stop Services

```bash
# Stop tất cả services
docker-compose down

# Stop và xóa volumes (careful!)
docker-compose down -v

# Stop và xóa images
docker-compose down --rmi all
```

## 📝 Logs Location

- API logs: Container stdout/stderr
- Worker logs: Container stdout/stderr  
- Redis logs: Container stdout/stderr
- Host system logs: `/var/log/docker/`

---

**Developed by LuckTools Team**

Để được hỗ trợ, vui lòng tạo issue trên repository hoặc liên hệ team development.