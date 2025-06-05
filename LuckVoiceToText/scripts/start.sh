#!/bin/bash

# Whisper API - Management script optimized for Ubuntu 22.04
# Version: 1.0.0

# Màu sắc cho output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Default values
DEFAULT_CPU_PER_WORKER=2.5
DEFAULT_RAM_PER_WORKER=4000

# Kiểm tra Docker improved for Ubuntu 22.04
check_docker() {
    echo -e "${BLUE}[+] Kiểm tra Docker...${NC}"
    
    # Kiểm tra docker trong PATH
    if command -v docker > /dev/null 2>&1; then
        DOCKER_CMD="docker"
        echo -e "${GREEN}[✓] Docker đã cài đặt${NC}"
    else
        # Kiểm tra các vị trí phổ biến
        if [ -x "/usr/bin/docker" ]; then
            DOCKER_CMD="/usr/bin/docker"
            echo -e "${YELLOW}[!] Docker tìm thấy tại /usr/bin/docker${NC}"
        elif [ -x "/usr/local/bin/docker" ]; then
            DOCKER_CMD="/usr/local/bin/docker"
            echo -e "${YELLOW}[!] Docker tìm thấy tại /usr/local/bin/docker${NC}"
        elif [ -x "/snap/bin/docker" ]; then
            DOCKER_CMD="/snap/bin/docker"
            echo -e "${YELLOW}[!] Docker tìm thấy tại /snap/bin/docker${NC}"
        else
            echo -e "${RED}[!] Docker không tìm thấy. Vui lòng cài đặt Docker trước.${NC}"
            echo -e "${YELLOW}[i] Cài đặt: https://docs.docker.com/engine/install/ubuntu/${NC}"
            exit 1
        fi
    fi
    
    # Kiểm tra Docker daemon
    if ! $DOCKER_CMD info > /dev/null 2>&1; then
        echo -e "${YELLOW}[!] Docker daemon không chạy. Thử khởi động...${NC}"
        if command -v systemctl > /dev/null 2>&1; then
            sudo systemctl start docker || true
            sleep 2
            if ! $DOCKER_CMD info > /dev/null 2>&1; then
                echo -e "${RED}[!] Không thể khởi động Docker daemon.${NC}"
                exit 1
            fi
        else
            echo -e "${RED}[!] Không thể khởi động Docker daemon. Thử:${NC}"
            echo -e "${YELLOW}    sudo service docker start${NC}"
            exit 1
        fi
    fi
    
    echo -e "${GREEN}[✓] Docker daemon đang chạy${NC}"
    
    # Kiểm tra Docker Compose
    if $DOCKER_CMD compose > /dev/null 2>&1; then
        DOCKER_COMPOSE_CMD="$DOCKER_CMD compose"
        echo -e "${GREEN}[✓] Docker Compose plugin đã cài đặt${NC}"
    elif command -v docker-compose > /dev/null 2>&1; then
        DOCKER_COMPOSE_CMD="docker-compose"
        echo -e "${GREEN}[✓] Docker Compose standalone đã cài đặt${NC}"
    else
        echo -e "${YELLOW}[!] Docker Compose không tìm thấy. Cài đặt:${NC}"
        echo -e "${YELLOW}    sudo apt-get install docker-compose-plugin${NC}"
        exit 1
    fi
}

# Kiểm tra file docker-compose.yml tồn tại
check_compose_file() {
    if [ ! -f "docker-compose.yml" ]; then
        echo -e "${RED}[!] Không tìm thấy file docker-compose.yml trong thư mục hiện tại.${NC}"
        echo -e "${YELLOW}Đảm bảo bạn đang ở đúng thư mục chứa docker-compose.yml${NC}"
        exit 1
    fi
}

# Hiển thị hướng dẫn sử dụng
show_usage() {
    echo -e "${BLUE}Whisper API Management - System Configuration for Ubuntu 22.04${NC}"
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  start                  Khởi động toàn bộ hệ thống"
    echo "  start-core             Chỉ khởi động các dịch vụ core (không giám sát)"
    echo "  start-monitoring       Chỉ khởi động hệ thống giám sát"
    echo "  stop                   Dừng toàn bộ dịch vụ"
    echo "  restart                Khởi động lại toàn bộ dịch vụ"
    echo "  status                 Hiển thị trạng thái hệ thống"
    echo "  logs [service]         Xem logs (api, worker, redis hoặc tất cả)"
    echo "  scale <workers>        Điều chỉnh số lượng worker (2-4 workers khuyến nghị)"
    echo "  update                 Cập nhật image và khởi động lại dịch vụ"
    echo "  check-resources        Kiểm tra tài nguyên hệ thống"
    echo "  healthcheck            Kiểm tra trạng thái API qua endpoint /health"
    echo ""
    echo "Examples:"
    echo "  $0 start               # Khởi động toàn bộ hệ thống"
    echo "  $0 scale 3             # Điều chỉnh số lượng worker thành 3"
    echo "  $0 logs worker         # Xem logs của worker"
}

# Kiểm tra tài nguyên hệ thống
check_resources() {
    echo -e "${BLUE}[+] Kiểm tra tài nguyên hệ thống...${NC}"
    
    # CPU Cores
    CPU_CORES=$(nproc 2>/dev/null || grep -c ^processor /proc/cpuinfo 2>/dev/null || echo "N/A")
    echo -e "${GREEN}[✓] CPU: ${CPU_CORES} cores${NC}"
    
    # RAM
    if command -v free &> /dev/null; then
        TOTAL_RAM=$(free -m | awk '/^Mem:/{print $2}')
        FREE_RAM=$(free -m | awk '/^Mem:/{print $7}')
        USED_RAM=$((TOTAL_RAM - FREE_RAM))
        echo -e "${GREEN}[✓] RAM: ${USED_RAM}MB đã sử dụng / ${TOTAL_RAM}MB tổng cộng (${FREE_RAM}MB còn trống)${NC}"
    else
        echo -e "${YELLOW}[!] Không thể kiểm tra RAM (lệnh 'free' không có sẵn)${NC}"
    fi
    
    # Disk
    if command -v df &> /dev/null; then
        DISK_USAGE=$(df -h . | awk 'NR==2 {print $5}')
        DISK_FREE=$(df -h . | awk 'NR==2 {print $4}')
        echo -e "${GREEN}[✓] Disk: Đã sử dụng ${DISK_USAGE} (${DISK_FREE} còn trống)${NC}"
    else
        echo -e "${YELLOW}[!] Không thể kiểm tra disk (lệnh 'df' không có sẵn)${NC}"
    fi
    
    # Docker containers
    if [[ -n "$DOCKER_CMD" ]]; then
        DOCKER_COUNT=$($DOCKER_CMD ps -q | wc -l)
        echo -e "${GREEN}[✓] Docker: ${DOCKER_COUNT} container đang chạy${NC}"
    fi
    
    return 0
}

# Kiểm tra API key từ file .env
check_api_key() {
    if [ -f .env ]; then
        # Use grep with -F to match the fixed string "API_KEYS"
        API_KEY=$(grep -F "API_KEYS" .env | cut -d= -f2)
        if [ -n "$API_KEY" ]; then
            echo -e "${GREEN}[✓] API Key: ${API_KEY}${NC}"
        else
            echo -e "${YELLOW}[!] Không tìm thấy API Key trong file .env${NC}"
        fi
    else
        echo -e "${YELLOW}[!] Không tìm thấy file .env${NC}"
    fi
}

# Xác định số lượng worker phù hợp với tài nguyên
recommend_workers() {
    CPU_CORES=$(nproc 2>/dev/null || grep -c ^processor /proc/cpuinfo 2>/dev/null || echo "0")
    
    if command -v free &> /dev/null; then
        TOTAL_RAM=$(free -m | awk '/^Mem:/{print $2}')
    else
        TOTAL_RAM=0
        echo -e "${YELLOW}[!] Không thể kiểm tra RAM. Đề xuất worker có thể không chính xác.${NC}"
    fi
    
    # Trừ đi CPU và RAM cho API và Redis
    AVAILABLE_CPU=$((CPU_CORES - 3))  # Trừ 1.5 cho API và 1.5 cho Redis+khác
    AVAILABLE_RAM=$((TOTAL_RAM - 4000))  # Trừ 2GB cho API và 2GB cho Redis+khác
    
    # Đảm bảo giá trị không âm
    if [ $AVAILABLE_CPU -lt 0 ]; then AVAILABLE_CPU=0; fi
    if [ $AVAILABLE_RAM -lt 0 ]; then AVAILABLE_RAM=0; fi
    
    # Tính số worker tối đa dựa trên CPU
    MAX_WORKERS_CPU=$(echo "$AVAILABLE_CPU / $DEFAULT_CPU_PER_WORKER" | bc -l | awk '{print int($1)}')
    
    # Tính số worker tối đa dựa trên RAM
    MAX_WORKERS_RAM=$(echo "$AVAILABLE_RAM / $DEFAULT_RAM_PER_WORKER" | bc -l | awk '{print int($1)}')
    
    # Lấy giá trị nhỏ hơn
    if [ $MAX_WORKERS_CPU -lt $MAX_WORKERS_RAM ]; then
        MAX_WORKERS=$MAX_WORKERS_CPU
    else
        MAX_WORKERS=$MAX_WORKERS_RAM
    fi
    
    # Đảm bảo ít nhất 1 worker
    if [ $MAX_WORKERS -lt 1 ]; then
        MAX_WORKERS=1
    fi
    
    # Đề xuất
    echo -e "${BLUE}[i] Dựa trên tài nguyên hệ thống (${CPU_CORES} CPU, ${TOTAL_RAM}MB RAM)${NC}"
    echo -e "${BLUE}[i] Số lượng worker đề xuất: ${MAX_WORKERS} (tối đa)${NC}"
    
    # Get current worker count
    CURRENT_WORKERS=$($DOCKER_COMPOSE_CMD ps 2>/dev/null | grep -c "whisper-api_worker")
    
    # If the command failed or returned 0, try alternative method
    if [ $? -ne 0 ] || [ $CURRENT_WORKERS -eq 0 ]; then
        CURRENT_WORKERS=$($DOCKER_CMD ps --format "{{.Names}}" 2>/dev/null | grep -c "worker")
    fi
    
    echo -e "${BLUE}[i] Số lượng worker hiện tại: ${CURRENT_WORKERS}${NC}"
}

# Khởi động toàn bộ hệ thống
start_system() {
    echo -e "${BLUE}[+] Khởi động Whisper API...${NC}"
    $DOCKER_COMPOSE_CMD up -d
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}[✓] Whisper API đã được khởi động${NC}"
        
        # Hiển thị thông tin truy cập
        SERVER_IP=$(hostname -I 2>/dev/null | awk '{print $1}' || echo "localhost")
        echo -e "${GREEN}[✓] API URL: http://${SERVER_IP}:8000/transcribe/${NC}"
        echo -e "${GREEN}[✓] Grafana URL: http://${SERVER_IP}:3000/ (admin/whisper_admin_password)${NC}"
        
        # Hiển thị API key
        check_api_key
    else
        echo -e "${RED}[!] Không thể khởi động Whisper API. Kiểm tra logs để biết thêm chi tiết.${NC}"
        echo -e "${YELLOW}[i] Sử dụng: $0 logs${NC}"
    fi
}

# Chỉ khởi động dịch vụ core, không bao gồm giám sát
start_core() {
    echo -e "${BLUE}[+] Khởi động các dịch vụ core (API + Worker + Redis)...${NC}"
    $DOCKER_COMPOSE_CMD up -d api worker redis
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}[✓] Dịch vụ core đã được khởi động${NC}"
        
        SERVER_IP=$(hostname -I 2>/dev/null | awk '{print $1}' || echo "localhost")
        echo -e "${GREEN}[✓] API URL: http://${SERVER_IP}:8000/transcribe/${NC}"
        
        # Hiển thị API key
        check_api_key
    else
        echo -e "${RED}[!] Không thể khởi động dịch vụ core. Kiểm tra logs để biết thêm chi tiết.${NC}"
    fi
}

# Chỉ khởi động dịch vụ giám sát
start_monitoring() {
    echo -e "${BLUE}[+] Khởi động hệ thống giám sát...${NC}"
    $DOCKER_COMPOSE_CMD up -d node-exporter cadvisor redis-exporter prometheus grafana
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}[✓] Hệ thống giám sát đã được khởi động${NC}"
        
        SERVER_IP=$(hostname -I 2>/dev/null | awk '{print $1}' || echo "localhost")
        echo -e "${GREEN}[✓] Grafana URL: http://${SERVER_IP}:3000/ (admin/whisper_admin_password)${NC}"
        echo -e "${GREEN}[✓] Prometheus URL: http://${SERVER_IP}:9090/${NC}"
    else
        echo -e "${RED}[!] Không thể khởi động hệ thống giám sát. Kiểm tra logs để biết thêm chi tiết.${NC}"
    fi
}

# Dừng toàn bộ dịch vụ
stop_system() {
    echo -e "${BLUE}[+] Dừng toàn bộ dịch vụ...${NC}"
    $DOCKER_COMPOSE_CMD down
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}[✓] Đã dừng toàn bộ dịch vụ${NC}"
    else
        echo -e "${RED}[!] Gặp lỗi khi dừng dịch vụ. Kiểm tra logs để biết thêm chi tiết.${NC}"
    fi
}

# Khởi động lại toàn bộ dịch vụ
restart_system() {
    echo -e "${BLUE}[+] Khởi động lại toàn bộ dịch vụ...${NC}"
    $DOCKER_COMPOSE_CMD restart
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}[✓] Đã khởi động lại toàn bộ dịch vụ${NC}"
    else
        echo -e "${RED}[!] Gặp lỗi khi khởi động lại dịch vụ. Kiểm tra logs để biết thêm chi tiết.${NC}"
    fi
}

# Hiển thị trạng thái hệ thống
show_status() {
    echo -e "${BLUE}[+] Trạng thái hệ thống:${NC}"
    $DOCKER_COMPOSE_CMD ps
}

# Xem logs
view_logs() {
    SERVICE=$1
    
    if [ -z "$SERVICE" ]; then
        echo -e "${BLUE}[+] Hiển thị logs của toàn bộ dịch vụ:${NC}"
        $DOCKER_COMPOSE_CMD logs --tail=100 -f
    else
        if [ "$SERVICE" == "api" ] || [ "$SERVICE" == "worker" ] || [ "$SERVICE" == "redis" ]; then
            echo -e "${BLUE}[+] Hiển thị logs của dịch vụ $SERVICE:${NC}"
            $DOCKER_COMPOSE_CMD logs --tail=100 -f $SERVICE
        else
            echo -e "${RED}[!] Dịch vụ không hợp lệ. Vui lòng chọn: api, worker, redis${NC}"
        fi
    fi
}

# Điều chỉnh số lượng worker
scale_workers() {
    if [ -z "$1" ]; then
        echo -e "${RED}[!] Vui lòng cung cấp số lượng worker${NC}"
        return 1
    fi
    
    NUM_WORKERS=$1
    
    # Kiểm tra giới hạn
    if [ $NUM_WORKERS -lt 1 ]; then
        echo -e "${RED}[!] Số lượng worker tối thiểu là 1${NC}"
        return 1
    fi
    
    # Kiểm tra tài nguyên trước khi scale
    CPU_CORES=$(nproc 2>/dev/null || grep -c ^processor /proc/cpuinfo 2>/dev/null || echo "0")
    
    if command -v free &> /dev/null; then
        TOTAL_RAM=$(free -m | awk '/^Mem:/{print $2}')
    else
        TOTAL_RAM=0
        echo -e "${YELLOW}[!] Không thể kiểm tra RAM. Cẩn thận khi scale.${NC}"
    fi
    
    # Use bc for floating point calculation if available
    if command -v bc &> /dev/null; then
        REQUIRED_CPU=$(echo "$DEFAULT_CPU_PER_WORKER * $NUM_WORKERS + 2.5" | bc)
        REQUIRED_RAM=$(echo "$DEFAULT_RAM_PER_WORKER * $NUM_WORKERS + 4000" | bc)
        REQUIRED_RAM=${REQUIRED_RAM%.*}  # Xóa phần thập phân
        
        if (( $(echo "$REQUIRED_CPU > $CPU_CORES" | bc -l) )); then
            echo -e "${YELLOW}[!] Cảnh báo: Scale lên $NUM_WORKERS worker cần khoảng $REQUIRED_CPU cores CPU${NC}"
            echo -e "${YELLOW}[!] Hệ thống hiện có $CPU_CORES cores CPU${NC}"
            echo -ne "${YELLOW}[?] Bạn có chắc chắn muốn tiếp tục? (y/n): ${NC}"
            read CONFIRM
            if [ "$CONFIRM" != "y" ]; then
                echo -e "${RED}[!] Đã hủy thao tác${NC}"
                return 1
            fi
        fi
        
        if [ $REQUIRED_RAM -gt $TOTAL_RAM ]; then
            echo -e "${YELLOW}[!] Cảnh báo: Scale lên $NUM_WORKERS worker cần khoảng $REQUIRED_RAM MB RAM${NC}"
            echo -e "${YELLOW}[!] Hệ thống hiện có $TOTAL_RAM MB RAM${NC}"
            echo -ne "${YELLOW}[?] Bạn có chắc chắn muốn tiếp tục? (y/n): ${NC}"
            read CONFIRM
            if [ "$CONFIRM" != "y" ]; then
                echo -e "${RED}[!] Đã hủy thao tác${NC}"
                return 1
            fi
        fi
    else
        echo -e "${YELLOW}[!] Không thể tính toán tài nguyên cần thiết (lệnh 'bc' không có sẵn)${NC}"
        echo -e "${YELLOW}[!] Cẩn thận khi scale lên số lượng worker cao.${NC}"
    fi
    
    echo -e "${BLUE}[+] Điều chỉnh số lượng worker thành $NUM_WORKERS...${NC}"
    $DOCKER_COMPOSE_CMD up -d --scale worker=$NUM_WORKERS
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}[✓] Đã điều chỉnh số lượng worker thành $NUM_WORKERS${NC}"
    else
        echo -e "${RED}[!] Không thể scale worker. Kiểm tra logs để biết thêm chi tiết.${NC}"
    fi
}

# Cập nhật hệ thống
update_system() {
    echo -e "${BLUE}[+] Cập nhật Whisper API...${NC}"
    echo -e "${BLUE}[+] Pulling các image mới nhất...${NC}"
    $DOCKER_COMPOSE_CMD pull
    
    echo -e "${BLUE}[+] Xây dựng lại các image...${NC}"
    $DOCKER_COMPOSE_CMD build
    
    echo -e "${BLUE}[+] Khởi động lại hệ thống với các image mới nhất...${NC}"
    $DOCKER_COMPOSE_CMD up -d
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}[✓] Whisper API đã được cập nhật thành công${NC}"
    else
        echo -e "${RED}[!] Gặp lỗi khi cập nhật. Kiểm tra logs để biết thêm chi tiết.${NC}"
    fi
}

# Kiểm tra sức khỏe API
check_health() {
    echo -e "${BLUE}[+] Kiểm tra sức khỏe API...${NC}"
    
    # Kiểm tra API key
    if [ -f .env ]; then
        API_KEY=$(grep -F "API_KEYS" .env 2>/dev/null | cut -d= -f2 | tr -d ',')
        if [ -z "$API_KEY" ]; then
            API_KEY="test_key"
            echo -e "${YELLOW}[!] Không tìm thấy API key trong file .env, sử dụng 'test_key' mặc định${NC}"
        fi
    else
        API_KEY="test_key"
        echo -e "${YELLOW}[!] Không tìm thấy file .env, sử dụng API key mặc định${NC}"
    fi
    
    # Kiểm tra IP máy chủ
    SERVER_IP=$(hostname -I 2>/dev/null | awk '{print $1}' || echo "localhost")
    
    # Kiểm tra API endpoint
    echo -e "${BLUE}[+] Gọi endpoint /health...${NC}"
    
    # Kiểm tra curl có sẵn không
    if command -v curl &> /dev/null; then
        # Kiểm tra jq có sẵn không
        if command -v jq &> /dev/null; then
            curl -s -X GET "http://${SERVER_IP}:8000/health" -H "X-API-Key: ${API_KEY}" | jq || echo -e "${RED}[!] Không thể kết nối đến API${NC}"
        else
            echo -e "${YELLOW}[!] Công cụ 'jq' không có sẵn, hiển thị kết quả thô${NC}"
            curl -s -X GET "http://${SERVER_IP}:8000/health" -H "X-API-Key: ${API_KEY}" || echo -e "${RED}[!] Không thể kết nối đến API${NC}"
        fi
    else
        echo -e "${RED}[!] Công cụ 'curl' không có sẵn. Không thể kiểm tra sức khỏe API.${NC}"
        echo -e "${YELLOW}[i] Cài đặt curl: sudo apt-get install curl${NC}"
    fi
}

# Kiểm tra quyền
check_permissions() {
    # Check if script is run as root and warn if necessary
    if [ "$(id -u)" -eq 0 ]; then
        echo -e "${YELLOW}[!] Script đang chạy với quyền root. Điều này có thể gây ra vấn đề với Docker permissions.${NC}"
        echo -e "${YELLOW}[!] Khuyến nghị chạy script với user thông thường trong nhóm docker.${NC}"
    fi
    
    # Check for write permission in current directory
    if [ ! -w "." ]; then
        echo -e "${RED}[!] Không có quyền ghi trong thư mục hiện tại.${NC}"
        echo -e "${YELLOW}[!] Thực hiện: chmod u+w .${NC}"
        return 1
    fi
    
    return 0
}

# Main function
main() {
    # Check script permissions
    check_permissions
    
    # Check docker before doing any other operations
    check_docker
    
    # Check compose file exists
    check_compose_file
    
    case "$1" in
        start)
            start_system
            ;;
        start-core)
            start_core
            ;;
        start-monitoring)
            start_monitoring
            ;;
        stop)
            stop_system
            ;;
        restart)
            restart_system
            ;;
        status)
            show_status
            ;;
        logs)
            view_logs "$2"
            ;;
        scale)
            scale_workers "$2"
            ;;
        update)
            update_system
            ;;
        check-resources)
            check_resources
            recommend_workers
            ;;
        healthcheck)
            check_health
            ;;
        *)
            show_usage
            ;;
    esac
}

# Set up variables to avoid using them before definition
DOCKER_CMD="docker"
DOCKER_COMPOSE_CMD="docker compose"

# Run main function with all arguments
main "$@"