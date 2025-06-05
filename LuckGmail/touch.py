import subprocess
import sys
import time

def get_connected_devices():
    """Trả về danh sách serial các thiết bị đã kết nối adb."""
    try:
        output = subprocess.check_output("adb devices", shell=True).decode('utf-8')
        lines = output.strip().split('\n')[1:]
        devices = [line.split()[0] for line in lines if '\tdevice' in line]
        return devices
    except Exception:
        return []

def select_device():
    """Cho phép chọn thiết bị nếu có nhiều thiết bị."""
    devices = get_connected_devices()
    if not devices:
        print("Không tìm thấy thiết bị nào.")
        sys.exit(1)
    if len(devices) == 1:
        return devices[0]
    print("Danh sách thiết bị đã kết nối:")
    for i, serial in enumerate(devices):
        print(f"{i+1}. {serial}")
    idx = input(f"Chọn thiết bị (1-{len(devices)}): ")
    try:
        idx = int(idx) - 1
        if 0 <= idx < len(devices):
            return devices[idx]
    except:
        pass
    print("Lựa chọn không hợp lệ, sử dụng thiết bị đầu tiên.")
    return devices[0]

def enable_pointer_location(serial):
    subprocess.call(f'adb -s {serial} shell settings put system pointer_location 1', shell=True)
    print("Đã bật hiển thị tọa độ con trỏ trên thiết bị.")

def disable_pointer_location(serial):
    subprocess.call(f'adb -s {serial} shell settings put system pointer_location 0', shell=True)
    print("Đã tắt hiển thị tọa độ con trỏ.")

def main():
    serial = select_device()
    enable_pointer_location(serial)
    print("\n=== ĐANG THEO DÕI TỌA ĐỘ CHẠM ===")
    print("-> Xem trực tiếp tọa độ trên màn hình điện thoại (hàng đầu ở góc trên)")
    print("-> Nhấn Ctrl+C để thoát.")

    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print("\nĐã thoát.")
    finally:
        disable_pointer_location(serial)

if __name__ == "__main__":
    main()

