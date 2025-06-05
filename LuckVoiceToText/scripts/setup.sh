#!/bin/bash
# LuckAI API - Script cài đặt tự động cho VPS 10 CPU, 16GB RAM
# Script này giả định các file cấu hình đã tồn tại

set -e  # Dừng script nếu có lỗi

# Màu sắc cho output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Kiểm tra quyền root
check_root() {
    echo -e "${BLUE}[+] Kiểm tra quyền root...${NC}"
    if [ "$(id -u)" -ne 0 ]; then
        echo -e "${RED}[!] Script này yêu cầu quyền root. Vui lòng chạy với sudo${NC}"
        exit 1
    fi
    echo -e "${GREEN}[✓] Quyền root: OK${NC}"
}

# Kiểm tra cấu hình hệ thống
check_system() {
    echo -e "${BLUE}[+] Kiểm tra cấu hình hệ thống...${NC}"
    
    # Kiểm tra CPU
    CPU_CORES=$(nproc)
    if [ "$CPU_CORES" -lt 8 ]; then
        echo -e "${YELLOW}[!] Cảnh báo: Hệ thống có $CPU_CORES CPU cores. Khuyến nghị: 10 CPU cores${NC}"
    else
        echo -e "${GREEN}[✓] CPU: $CPU_CORES cores${NC}"
    fi
    
    # Kiểm tra RAM
    TOTAL_RAM=$(free -m | awk '/^Mem:/{print $2}')
    if [ "$TOTAL_RAM" -lt 14000 ]; then
        echo -e "${YELLOW}[!] Cảnh báo: Hệ thống có ${TOTAL_RAM}MB RAM. Khuyến nghị: 16GB RAM${NC}"
    else
        echo -e "${GREEN}[✓] RAM: $TOTAL_RAM MB${NC}"
    fi
    
    # Kiểm tra không gian đĩa
    DISK_SPACE=$(df -h / | awk 'NR==2 {print $4}')
    echo -e "${GREEN}[✓] Không gian đĩa còn trống: $DISK_SPACE${NC}"
}

# Cài đặt Docker và Docker Compose
install_docker() {
    echo -e "${BLUE}[+] Cài đặt Docker...${NC}"
    
    # Kiểm tra Docker đã cài đặt chưa
    if command -v docker &> /dev/null && command -v docker &> /dev/null compose; then
        echo -e "${GREEN}[✓] Docker và Docker Compose đã được cài đặt${NC}"
        return
    fi
    
    # Cập nhật package index
    apt-get update
    
    # Cài đặt các gói phụ thuộc
    apt-get install -y \
        apt-transport-https \
        ca-certificates \
        curl \
        gnupg \
        lsb-release \
        jq
    
    # Thêm Docker GPG key
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
    
    # Thiết lập repository
    echo \
    "deb [arch=amd64 signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu \
    $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null
    
    # Cài đặt Docker
    apt-get update
    apt-get install -y docker-ce docker-ce-cli containerd.io
    
    # Cài đặt Docker Compose
    apt-get install -y docker-compose-plugin
    
    # Kiểm tra cài đặt
    docker --version
    docker compose version
    
    # Khởi động Docker
    systemctl enable docker
    systemctl start docker
    
    echo -e "${GREEN}[✓] Docker đã được cài đặt thành công${NC}"
}

# Tối ưu hóa hệ thống
optimize_system() {
    echo -e "${BLUE}[+] Tối ưu hóa hệ thống...${NC}"
    
    # Cấu hình sysctl
    cat >> /etc/sysctl.conf << EOF
# Tối ưu cho Docker và ứng dụng mạng
net.core.somaxconn = 4096
net.ipv4.tcp_max_syn_backlog = 4096
net.ipv4.ip_local_port_range = 1024 65535
net.ipv4.tcp_fin_timeout = 15
net.ipv4.tcp_keepalive_time = 300
net.ipv4.tcp_keepalive_probes = 5
net.ipv4.tcp_keepalive_intvl = 15

# Tối ưu sử dụng RAM
vm.swappiness = 10
vm.dirty_ratio = 20
vm.dirty_background_ratio = 5
EOF
    
    # Áp dụng cấu hình sysctl
    sysctl -p
    
    # Cấu hình limits
    cat >> /etc/security/limits.conf << EOF
* soft nofile 65535
* hard nofile 65535
EOF
    
    echo -e "${GREEN}[✓] Đã tối ưu hóa hệ thống${NC}"
}

# Cấu hình API key
configure_api_keys() {
    echo -e "${BLUE}[+] Cấu hình API key...${NC}"
    
    # Kiểm tra xem file .env đã tồn tại chưa
    if [ -f .env ]; then
        echo -e "${YELLOW}[!] File .env đã tồn tại. Giữ nguyên cấu hình.${NC}"
        return
    fi
    
    # Tạo API key ngẫu nhiên
    API_KEY=$(openssl rand -hex 16)
    
    # Tạo file .env
    cat > .env << EOF
API_KEYS=${API_KEY}
MODEL_SIZE=small
EOF
    
    echo -e "${GREEN}[✓] Đã tạo API key: ${API_KEY}${NC}"
    echo -e "${GREEN}[✓] Lưu ý: Hãy lưu lại API key này để sử dụng trong ứng dụng của bạn${NC}"
}

# Khởi động hệ thống
start_system() {
    echo -e "${BLUE}[+] Khởi động LuckAI API...${NC}"
    
    # Kiểm tra xem docker-compose.yml có tồn tại không
    if [ ! -f docker-compose.yml ]; then
        echo -e "${RED}[!] Không tìm thấy file docker-compose.yml. Vui lòng kiểm tra lại.${NC}"
        exit 1
    fi
    
    # Pull images và khởi động containers
    docker compose pull
    docker compose up -d
    
    # Kiểm tra trạng thái
    sleep 5
    docker compose ps
    
    echo -e "${GREEN}[✓] LuckAI API đã được khởi động thành công${NC}"
    echo -e "${GREEN}[✓] Có thể truy cập API tại: http://<your-server-ip>:8000${NC}"
    echo -e "${GREEN}[✓] Truy cập Grafana (nếu đã cài đặt) tại: http://<your-server-ip>:3000${NC}"
    
    # Hiển thị thông tin API key
    if [ -f .env ]; then
        API_KEY=$(grep API_KEYS .env | cut -d= -f2)
        echo -e "${GREEN}[✓] API Key: ${API_KEY}${NC}"
    fi
}

# Hàm chính
main() {
    echo -e "${BLUE}==== Cài đặt LuckAI API (10 CPU, 16GB RAM) ====${NC}"
    
    # Kiểm tra quyền root
    check_root
    
    # Kiểm tra cấu hình hệ thống
    check_system
    
    # Cài đặt Docker
    install_docker
    
    # Tối ưu hóa hệ thống
    optimize_system
    
    # Cấu hình API key
    configure_api_keys
    
    # Khởi động hệ thống
    start_system
    
    echo -e "${BLUE}==== Cài đặt hoàn tất ====${NC}"
    echo -e "${GREEN}[✓] LuckAI API đã sẵn sàng sử dụng${NC}"
}

# Chạy hàm chính
main