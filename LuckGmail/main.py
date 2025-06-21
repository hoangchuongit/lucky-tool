import random
import string
import time
import pyautogui
import subprocess
import pygetwindow as gw
import os
import requests
import xml.etree.ElementTree as ET
import threading
import hashlib
import psycopg2
import datetime
import logging
from logging.handlers import TimedRotatingFileHandler
from faker import Faker
fake = Faker('en_US')
import re
import win32gui
import win32con
import win32com.client

os.makedirs('logs', exist_ok=True)

# Set up logging to output to both file and console
log_formatter = logging.Formatter('%(asctime)-15s %(levelname)-8s %(message)s')
console_handler = logging.StreamHandler()
console_handler.setFormatter(log_formatter)
file_handler = TimedRotatingFileHandler(
    filename="logs/app.log",
    when="H",
    interval=1,
    backupCount=24,
    encoding="utf-8",
    utc=False
)
file_handler.setFormatter(log_formatter)
logging.basicConfig(level=logging.INFO, handlers=[console_handler, file_handler])

def generate_random_serial():
    """Tạo số serial ngẫu nhiên theo định dạng R**A**KK**."""
    return f"R{random.randint(1111111111, 9999999999)}"

def generate_imei():
    imei_prefix = "35785707"  # 6 chữ số cố định
    random_numbers = [random.randint(0, 9) for _ in range(6)]  # 8 chữ số ngẫu nhiên
    imei_body = imei_prefix + ''.join(map(str, random_numbers))  # Tổng cộng 14 chữ số

    def calculate_luhn(imei):
        digits = [int(d) for d in imei]
        for i in range(len(digits) - 1, -1, -2):
            digits[i] *= 2
            if digits[i] > 9:
                digits[i] -= 9
        total_sum = sum(digits)
        return (10 - (total_sum % 10)) % 10

    check_digit = calculate_luhn(imei_body + "0")  # Thêm "0" tạm để tính Luhn
    full_imei = imei_body + str(check_digit)  # 14 + 1 = 15 chữ số

    return full_imei

def generate_random_mac():
    samsung_oui_list = [
        [0x00, 0x1A, 0x2B], [0x00, 0x16, 0x6B], [0x00, 0x12, 0x47],
        [0x08, 0x37, 0x3D], [0x5C, 0x49, 0x79], [0x64, 0x77, 0x91],
        [0xAC, 0x5A, 0xD3], [0xB0, 0xC4, 0xE7], [0xCC, 0x3A, 0x61],
        [0xE0, 0xCB, 0x4E]
    ]
    oui = random.choice(samsung_oui_list)
    nic = [random.randint(0x00, 0xFF) for _ in range(3)]
    mac_address = oui + nic
    return ':'.join(f'{byte:02X}' for byte in mac_address)

def check_adb_devices():
    """
    Kiểm tra xem có thiết bị kết nối ADB.
    Trả về:
        - "ok" nếu có thiết bị usable (device hoặc recovery)
        - "offline" nếu có thiết bị nhưng ở trạng thái offline
        - "not_found" nếu không có thiết bị nào
        - "error" nếu có lỗi xảy ra khi chạy lệnh
    """
    try:
        output = subprocess.check_output("adb devices", shell=True).decode("utf-8")
        lines = output.strip().splitlines()[1:]
        for line in lines:
            if not line.strip():
                continue
            if line.strip().endswith("\toffline"):
                logging.warning("⚠ Thiết bị đang OFFLINE.")
                return "offline"
            if line.strip().endswith("\tdevice") or line.strip().endswith("\trecovery"):
                logging.info("✅ Đã tìm thấy thiết bị ADB (trạng thái usable).")
                return "ok"
        logging.warning("❌ Không tìm thấy thiết bị ADB nào.")
        return "not_found"
    except Exception as e:
        logging.error(f"🚨 Lỗi khi kiểm tra thiết bị ADB: {e}")
        return "error"

def restart_adb_server():
    """Restart adb server (kill và start)."""
    logging.info("🔁 Đang restart ADB server (kill-server & start-server)...")
    subprocess.call("adb kill-server", shell=True)
    time.sleep(1)
    subprocess.call("adb start-server", shell=True)
    time.sleep(2)
    logging.info("✅ ADB server đã được khởi động lại.")

def run_adb_command(command):
    """
    Chạy lệnh ADB nếu có thiết bị.
    - Retry tối đa 30 lần (mỗi lần cách nhau 5 giây).
    - Nếu gặp thiết bị offline → restart ADB ngay.
    - Nếu hết 30 lần vẫn không thấy thiết bị → restart ADB và thử lại từ đầu.
    """
    while True:
        retries = 0
        while retries <= 30:
            status = check_adb_devices()
            if status == "ok":
                break
            elif status == "offline":
                logging.warning("⛔ Phát hiện thiết bị OFFLINE → restart ADB ngay.")
                restart_adb_server()
                retries = 0
                continue
            elif status == "error":
                logging.error("❌ Không thể kiểm tra thiết bị do lỗi. Sẽ thử lại...")
            else:
                logging.info(f"⏳ Thiết bị chưa kết nối, thử lại lần {retries}/30...")
            retries += 1
            time.sleep(5)
        if retries > 30:
            logging.warning("🚫 Không tìm thấy thiết bị sau 30 lần thử. Đang restart adb server và thử lại...")
            restart_adb_server()
        else:
            break
    try:
        logging.info(f"🚀 Chạy lệnh: {command}")
        return subprocess.check_output(command, shell=True, stderr=subprocess.STDOUT).decode('utf-8')
    except subprocess.CalledProcessError as e:
        logging.error(f"❌ Lỗi khi chạy lệnh ADB: {e}")
        return e.output.decode('utf-8')





def get_window_coordinates_and_focus():
    global win_x, win_y
    logging.info("Đang tìm cửa sổ Z3X Samsung Tool PRO...")
    try:
        windows = [win for win in gw.getAllWindows() if win.title.strip()]
        target_window = next(
            (win for win in windows if 'z3x samsung tool pro' in win.title.lower()), None)
        if not target_window:
            logging.info("Không tìm thấy cửa sổ Z3X Samsung Tool PRO")
            return False
        logging.info("Đã tìm thấy cửa sổ, đang focus...")
        if not target_window.isActive:
            target_window.activate()
            logging.info("Đã focus vào cửa sổ.")
            time.sleep(2)
        fixed_left = 100
        fixed_top = 100
        fixed_width = 850
        fixed_height = 650
        target_window.moveTo(fixed_left, fixed_top)
        target_window.resizeTo(fixed_width, fixed_height)
        time.sleep(2)
        return True
    except Exception as e:
        logging.info(f"Đã xảy ra lỗi khi lấy tọa độ/focus cửa sổ: {e}")
        return False

def click_at_coordinates(x, y):
    logging.info(f"Di chuyển chuột tới ({x},{y})")
    try:
        pyautogui.click(x, y, duration=0.5)
        logging.info(f"Đã tới vị trí ({x}, {y}), click!")
        time.sleep(2)
    except Exception as e:
        logging.info(f"Đã xảy ra lỗi khi click: {e}")





def find_text_on_screen(target_text="Bắt đầu", on_screen=False, timeout=None, auto_click=False):
    count = 0
    while True:
        try:
            if on_screen:
                # Lệnh làm sáng màn hình
                run_adb_command("adb shell input keyevent 224")
                time.sleep(3) 

            run_adb_command("adb shell uiautomator dump /sdcard/ui.xml")
            run_adb_command("adb pull /sdcard/ui.xml")
            tree = ET.parse("ui.xml")
            root = tree.getroot()
            for node in root.iter('node'):
                text = node.attrib.get('text', '')
                desc = node.attrib.get('content-desc', '')
                bounds = node.attrib.get('bounds', '')
                if (target_text.lower() in text.lower()) or (target_text.lower() in desc.lower()):
                    logging.info(f'🎯 Đã tìm thấy: "{text or desc}"')
                    if auto_click and bounds:
                        import re
                        match = re.findall(r'\[(\d+),(\d+)\]', bounds)
                        if len(match) == 2:
                            x1, y1 = map(int, match[0])
                            x2, y2 = map(int, match[1])
                            x = (x1 + x2) // 2
                            y = (y1 + y2) // 2
                            logging.info(f'👉 Click vào tọa độ ({x}, {y})')
                            run_adb_command(f'adb shell input tap {x} {y}')
                    os.remove("ui.xml")
                    return True
        except Exception as e:
            logging.warning(f"Lỗi khi tìm text: {e}")
            if os.path.exists("ui.xml"):
                os.remove("ui.xml")
            logging.info(f'Không tìm thấy "{target_text}" trên màn hình. Thử lại sau 5s.')
            time.sleep(5)
        count += 1
        if timeout is not None and count >= timeout:
            logging.warning(f"⏰ Timeout: Không tìm thấy '{target_text}' sau {timeout} lần.")
            return False

def extract_first_gmail():
    while True:
        try:
            run_adb_command("adb shell input keyevent 224")
            run_adb_command("adb shell uiautomator dump /sdcard/ui.xml")
            run_adb_command("adb pull /sdcard/ui.xml")
            tree = ET.parse("ui.xml")
            root = tree.getroot()
            for node in root.iter('node'):
                text = node.attrib.get('text', '')
                if '@gmail.com' in text:
                    logging.info(f'Tìm thấy gmail: "{text}"')
                    os.remove("ui.xml")
                    return text
        except:
            if os.path.exists("ui.xml"):
                os.remove("ui.xml")
            time.sleep(5)

def random_input_swipe(x_range=(0, 100), y_range=(0, 100)):
    """Mô phỏng tap với tọa độ ngẫu nhiên"""
    try:
        x1 = random.randint(x_range[0], x_range[1])
        y1 = random.randint(y_range[0], y_range[1])
        run_adb_command(f'adb shell input tap {x1} {y1}')
    except Exception as e:
        logging.info(f"Lỗi khi thực hiện tap: {str(e)}")

def input_text_with_delay(text, min_delay=0.75, max_delay=1.75):
    """Nhập văn bản với độ trễ ngẫu nhiên giữa các ký tự"""
    try:
        for char in text:
            escaped_char = char.replace('"', '\\"').replace('\\', '\\\\')
            subprocess.Popen(f'adb shell input text "{escaped_char}"')
            time.sleep(random.uniform(min_delay, max_delay))
    except Exception as e:
        logging.info(f"Lỗi khi nhập text: {str(e)}")

def fake_realistic_gmail():
    while True:
        full_name = fake.name()
        parts = full_name.lower().split()
        if len(parts) >= 2:
            first_name = parts[0]
            last_name = parts[-1]
            username_formats = [
                f"{first_name}.{last_name}",
                f"{first_name}{last_name}",
                f"{first_name}.{last_name}{random.randint(10,99)}",
                f"{first_name}{random.randint(10,99)}{last_name}",
                f"{first_name}{random.randint(100,999)}",
                f"{last_name}{first_name}",
            ]
            email = random.choice(username_formats)
            return email

def screenshot_pull(filename="screen.png"):
    subprocess.call("adb shell screencap -p /sdcard/tmp_screen.png", shell=True)
    subprocess.call(f"adb pull /sdcard/tmp_screen.png {filename}", shell=True)
    subprocess.call("adb shell rm /sdcard/tmp_screen.png", shell=True)

def file_md5(path):
    with open(path, "rb") as f:
        return hashlib.md5(f.read()).hexdigest()

def human_like_scroll_to_bottom(max_scrolls=5):
    last_md5 = None
    for i in range(max_scrolls):
        screenshot_pull("screen_before.png")
        before_md5 = file_md5("screen_before.png")
        if before_md5 == last_md5:
            logging.info("Đã scroll đến cuối nội dung (ảnh màn hình không đổi).")
            return
        last_md5 = before_md5
        x = random.randint(520, 560)
        y1 = random.randint(1600, 1750)
        y2 = random.randint(350, 600)
        duration = random.randint(300, 700)
        cmd = f"adb shell input swipe {x} {y1} {x + random.randint(-8,8)} {y2} {duration}"
        logging.info(f"Scroll lần {i+1}: {cmd}")
        subprocess.call(cmd, shell=True)
        time.sleep(random.uniform(1.2, 2.3))
        time.sleep(2)
        screenshot_pull("screen_after.png")
        after_md5 = file_md5("screen_after.png")
        if before_md5 == after_md5:
            logging.info("Đã scroll đến cuối nội dung (ảnh màn hình không đổi).")
            return
    logging.info("Đã scroll hết số lần quy định (có thể chưa tới đáy).")

def step1():
    logging.info("📱 Màn hình 1 - Chào mừng")
    max_retry = 10
    for attempt in range(max_retry):
        logging.info(f"🔁 Thử lần {attempt + 1}/{max_retry}")
        try:
            # 1. Tìm "Bắt đầu", KHÔNG click tự động
            found = find_text_on_screen("Bắt đầu", on_screen=True, timeout=3, auto_click=False)
            if found:
                logging.info("👉 Đã tìm thấy 'Bắt đầu'. Tiến hành click thủ công theo tọa độ cố định.")
                # 2. Thực hiện swipe tap tại tọa độ bạn chỉ định
                x = random.randint(503, 568)
                y = random.randint(802, 842)
                duration = random.randint(55, 105)
                subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
                logging.info(f"📌 Đã tap tại ({x}, {y}). Chờ giao diện chuyển...")
                time.sleep(4)  # Đợi UI cập nhật
                # 3. Kiểm tra giao diện đã đổi chưa (không còn "Bắt đầu")
                still_there = find_text_on_screen("Bắt đầu", timeout=1)
                if not still_there:
                    logging.info("✅ Giao diện đã chuyển. Step 1 hoàn thành.")
                    return True
                else:
                    logging.warning("⚠️ Giao diện chưa đổi. Sẽ thử lại.")
            else:
                logging.warning("⏳ Không tìm thấy 'Bắt đầu'. Sẽ thử lại.")
        except Exception as e:
            logging.error(f"❌ Lỗi trong step1: {e}")
        time.sleep(1)
    logging.error("❌ Quá số lần thử. Step 1 thất bại.")
    return False

def step2():
    logging.info("Màn hình 2 - Kết nối wifi")
    if not find_text_on_screen("Tiếp theo", True, timeout=300):
        return False
    time.sleep(random.randint(2, 3))
    x = random.randint(752, 886)
    y = random.randint(1833, 1849)
    duration = random.randint(300, 700)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    return True

def step3():
    logging.info("Màn hình 3 - Đồng ý với các điều khoản điều kiện")
    if not find_text_on_screen("Đồng ý với các Điều khoản và Điều kiện", True, timeout=300):
        return False
    time.sleep(random.randint(2, 4))
    logging.info("Màn hình 3 - click để scroll xuống")
    x = random.randint(950, 1000)
    y = random.randint(1818, 1890)
    duration = random.randint(300, 700)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    time.sleep(random.randint(2, 4))
    logging.info("Màn hình 3 - Click tối đa đọc và đồng ý với tất cả nội dung trên")
    x = random.randint(112, 134)
    y = random.randint(1630, 1654)
    duration = random.randint(300, 700)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    time.sleep(random.randint(2, 4))
    logging.info("Màn hình 3 - Click nút tiếp theo")
    x = random.randint(756, 886)
    y = random.randint(1835, 1854)
    duration = random.randint(300, 700)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    return True

def step4():
    logging.info("Màn hình 4 - Đăng nhập")
    if not find_text_on_screen("Đăng nhập Sử dụng Tài khoản Google của bạn", timeout=300):
        return False
    time.sleep(random.randint(2, 4))
    x = random.randint(102, 309)
    y = random.randint(1453, 1463)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    return True

def step5():
    logging.info("Màn hình 5 - Dành cho mục đích cá nhân của tôi")
    if not find_text_on_screen("Dành cho mục đích cá nhân của tôi", timeout=300):
        return False
    time.sleep(random.randint(2, 4))
    x = random.randint(118, 677)
    y = random.randint(1018, 1024)
    duration = random.randint(300, 700)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    return True

def step6():
    logging.info("Màn hình 6 - Tạo tài khoản Google")
    if not find_text_on_screen("Tạo Tài khoản Google Nhập tên của bạn", timeout=300):
        return False
    name = fake.name()
    parts = name.split()
    logging.info("Màn hình 6 - Nhập Họ")
    first_name = parts[0]
    time.sleep(random.randint(2, 4))
    x = random.randint(126, 754)
    y = random.randint(682, 784)
    duration = random.randint(300, 700)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    input_text_with_delay(first_name)
    time.sleep(random.randint(2, 4))
    logging.info("Màn hình 6 - Nhập Tên")
    last_name = parts[-1]
    time.sleep(random.randint(2, 4))
    x = random.randint(118, 677)
    y = random.randint(1018, 1024)
    duration = random.randint(300, 700)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    input_text_with_delay(last_name)
    time.sleep(random.randint(2, 4))
    logging.info("Màn hình 6 - Click ra màn hình chính")
    x = random.randint(214, 880)
    y = random.randint(109, 206)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    time.sleep(random.randint(2, 4))
    logging.info("Màn hình 6 - Click button tiếp theo")
    x = random.randint(713, 976)
    y = random.randint(1757, 1820)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    return True

def step7():
    logging.info("Màn hình 7 - Thông tin cơ bản")
    if not find_text_on_screen("Thông tin cơ bản", timeout=300):
        return False
    time.sleep(random.randint(2, 4))
    logging.info("Màn hình 7 - Nhập ngày sinh")
    day = random.randint(1, 28)
    time.sleep(random.randint(2, 4))
    x = random.randint(138, 315)
    y = random.randint(704, 770)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    def input_text_slow(text, min_delay=0.75, max_delay=1.85):
        for char in text:
            input_text_with_delay(char)
            time.sleep(random.uniform(min_delay, max_delay))
    input_text_slow(str(day))
    logging.info("Màn hình 7 - Nhập tháng sinh")
    month = random.randint(1, 7)
    time.sleep(random.randint(2, 4))
    x = random.randint(452, 573)
    y = random.randint(696, 760)
    duration = random.randint(200, 400)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    time.sleep(random.randint(2, 4))
    thang_coords = [
        [(464, 702), (872, 956)],
        [(458, 700), (1020, 1097)],
        [(455, 700), (1159, 1238)],
        [(446, 689), (1302, 1389)],
        [(430, 692), (1442, 1536)],
        [(433, 696), (1589, 1677)],
        [(428, 694), (1715, 1810)],
    ]
    coords = thang_coords[month - 1]
    time.sleep(random.randint(2, 4))
    random_input_swipe(x_range=coords[0], y_range=coords[1])
    time.sleep(random.randint(2, 4))
    logging.info("Màn hình 7 - Nhập năm sinh")
    dob = fake.date_of_birth(minimum_age=19, maximum_age=20)
    year = dob.year
    time.sleep(random.randint(2, 4))
    x = random.randint(762, 961)
    y = random.randint(670, 779)
    duration = random.randint(200, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    input_text_slow(str(year))
    logging.info("Màn hình 7 - Nhập giới tính")
    gender = random.randint(1, 2)
    random_input_swipe(x_range=(130, 323), y_range=(917, 972))
    time.sleep(random.randint(2, 4))
    gender_coords = [
        [(110, 315), (1096, 1165)],
        [(118, 386), (1247, 1277)]
    ]
    coords_gender = gender_coords[gender - 1]
    random_input_swipe(x_range=coords_gender[0], y_range=coords_gender[1])
    time.sleep(random.randint(2, 4))
    logging.info("Màn hình 7 - Click button tiếp theo")
    x = random.randint(696, 967)
    y = random.randint(1761, 1807)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    return True

def step8():
    logging.info("Màn hình 8 - Chọn địa chỉ Gmail của bạn")
    if find_text_on_screen("Chọn địa chỉ Gmail của bạn", False, 3):
        logging.info("Màn hình 8 - Tìm: Tạo địa chỉ Gmail của riêng bạn")
        if not find_text_on_screen("Tạo địa chỉ Gmail của riêng bạn", timeout=300):
            return None
        time.sleep(random.randint(2, 4))
        logging.info("Màn hình 8 - Chọn gmail đầu tiên")
        x = random.randint(198, 501)
        y = random.randint(767, 785)
        duration = random.randint(300, 500)
        subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
        gmail = extract_first_gmail()
        if "@gmail.com" in gmail:
            final_gmail = gmail
        else:
            final_gmail = f"{gmail}@gmail.com"
        time.sleep(random.randint(2, 4))
        logging.info("Màn hình 8 - Click button tiếp theo")
        x = random.randint(689, 995)
        y = random.randint(1750, 1823)
        duration = random.randint(300, 500)
        subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
        return final_gmail
    else:
        logging.info("Màn hình 8 - Tạo một địa chỉ Gmail để đăng nhập vào Tài khoản Google của bạn")
        if not find_text_on_screen("Tạo một địa chỉ Gmail để đăng nhập vào Tài khoản Google của bạn", timeout=300):
            return None
        time.sleep(random.randint(2, 4))
        x = random.randint(104, 677)
        y = random.randint(744, 847)
        duration = random.randint(300, 500)
        subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
        time.sleep(random.randint(2, 4))

        # Hàm nhập ký tự từng chút một như người thật
        def input_text_slow_via_adb(text, min_delay=0.75, max_delay=1.85):
            for char in text:
                run_adb_command(f'adb shell input text "{char}"')
                time.sleep(random.uniform(min_delay, max_delay))

        # Hàm sinh Gmail sạch: không ký tự đặc biệt, chỉ a-z + số
        def generate_fake_gmail():
            first = fake.first_name().lower()
            last = fake.last_name().lower()
            number = ''.join(random.choices(string.digits, k=random.randint(4, 6)))
            raw = f"{first}{last}{number}"
            clean = re.sub(r'[^a-z0-9]', '', raw)  # chỉ giữ a-z và số
            return clean

        while True:
            gmail = generate_fake_gmail()
            time.sleep(random.randint(2, 4))
            input_text_slow_via_adb(gmail)
            logging.info("Màn hình 8 - Ấn nút BACK")
            run_adb_command("adb shell input keyevent 4")
            time.sleep(random.randint(2, 4))

            logging.info("Màn hình 8 - Click button tiếp theo")
            time.sleep(random.randint(2, 4))
            x = random.randint(693, 993)
            y = random.randint(1748, 1819)
            duration = random.randint(300, 500)
            subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
            if find_text_on_screen("Tên người dùng đã được sử dụng", False, 2):
                logging.info("⚠ Gmail bị trùng, thử lại")
                time.sleep(random.randint(1, 2))
                x = random.randint(104, 677)
                y = random.randint(744, 847)
                duration = random.randint(300, 500)
                subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
                run_adb_command("adb shell input keyevent 123")  # Move to end
                for _ in range(len(gmail)):
                    run_adb_command("adb shell input keyevent 67")  # Delete char
                time.sleep(2)
            else:
                break
        return f"{gmail}@gmail.com"

def step9():
    logging.info("Màn hình 9 - Tạo một mật khẩu mạnh")
    if not find_text_on_screen("Tạo một mật khẩu mạnh", timeout=300):
        return None
    
    time.sleep(random.randint(2, 3))
    # Nhấn vào ô nhập mật khẩu
    x = random.randint(136, 677)
    y = random.randint(748, 855)
    subprocess.call(f"adb shell input tap {x} {y}", shell=True)
    time.sleep(1)  # Đợi ô nhập được focus
    
    logging.info("Màn hình 9 - Sinh mật khẩu ngẫu nhiên")
    # Tạo mật khẩu an toàn (chỉ chữ cái in hoa, thường và số)
    safe_chars = string.ascii_lowercase + string.ascii_uppercase + string.digits
    password = ''.join(random.choices(safe_chars, k=random.randint(10, 14)))
    
    # Hàm nhập từng ký tự chậm, mô phỏng gõ người dùng
    def input_text_slow(text, min_delay=0.55, max_delay=1.75):
        entered_text = ""
        for char in text:
            subprocess.call(f'adb shell input text "{char}"', shell=True)
            entered_text += char
            time.sleep(random.uniform(min_delay, max_delay))
        return entered_text
    
    logging.info("Màn hình 9 - Bắt đầu nhập mật khẩu")
    entered_password = input_text_slow(password)
    
    # Kiểm tra mật khẩu đã nhập
    logging.info(f"Màn hình 9 - Mật khẩu sinh ra: {repr(password)}")
    logging.info(f"Màn hình 9 - Mật khẩu đã nhập: {repr(entered_password)}")
    
    # Nếu không khớp, xóa và nhập lại
    if entered_password != password:
        logging.info("Màn hình 9 - Mật khẩu không khớp, nhập lại")
        for _ in range(len(entered_password)):
            subprocess.call("adb shell input keyevent KEYCODE_DEL", shell=True)
            time.sleep(random.uniform(0.2, 0.4))
        entered_password = input_text_slow(password)
        logging.info(f"Màn hình 9 - Mật khẩu nhập lại: {repr(entered_password)}")
    
    # Kiểm tra lần cuối
    if entered_password != password:
        logging.error("Màn hình 9 - Lỗi: Mật khẩu nhập vẫn không khớp")
        return None
    
    logging.info(f"Màn hình 9 - Đã nhập mật khẩu thành công: {repr(password)}")
    
    # Thêm các bước tương tác giao diện từ code cũ
    logging.info("Màn hình 9 - Ấn nút BACK")
    subprocess.call("adb shell input keyevent 4", shell=True)
    time.sleep(random.randint(2, 4))
    
    logging.info("Màn hình 9 - Click button tiếp theo")
    x = random.randint(689, 979)
    y = random.randint(1744, 1819)
    subprocess.call(f"adb shell input tap {x} {y}", shell=True)
    time.sleep(random.randint(1, 2))
    return password



def step10():
    if find_text_on_screen("Xác nhận bạn không phải là rô bốt", False, 3):
        logging.info("Màn hình 10 - Xác minh CAPTCHA rô bốt, thoát")
        return False
    logging.info("Màn hình 10 - Thêm số điện thoại")
    if not find_text_on_screen("Thêm số điện thoại", timeout=300):
        return False
    time.sleep(random.randint(2, 4))
    logging.info("Màn hình 10 - Cuộn xuống dưới")
    human_like_scroll_to_bottom()
    logging.info("Màn hình 10 - Click lựa chọn khác")
    time.sleep(random.randint(2, 4))
    x = random.randint(86, 357)
    y = random.randint(1531, 1553)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    logging.info("Màn hình 10 - Chọn tùy chọn phù hợp với bạn")
    if not find_text_on_screen("Chọn tùy chọn phù hợp với bạn", timeout=300):
        return False
    time.sleep(random.randint(2, 4))
    logging.info("Màn hình 10 - Không, đừng thêm số điện thoại")
    x = random.randint(205, 463)
    y = random.randint(688, 709)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    time.sleep(random.randint(2, 4))
    logging.info("Màn hình 10 - Cuộn xuống dưới lần nữa")
    human_like_scroll_to_bottom()
    logging.info("Màn hình 10 - Click button xong")
    time.sleep(random.randint(2, 4))
    x = random.randint(774, 989)
    y = random.randint(1744, 1819)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    return True

def step11():
    logging.info("Màn hình 11 - Xem lại thông tin tài khoản của bạn")
    if not find_text_on_screen("Xem lại thông tin tài khoản của bạn", timeout=300):
        return False
    time.sleep(random.randint(2, 4))
    x = random.randint(703, 991)
    y = random.randint(1750, 1823)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    return True

def step12():
    logging.info("Màn hình 12 - Quyền riêng tư và điều khoản")
    if not find_text_on_screen("Quyền riêng tư và điều khoản", timeout=300):
        return False
    time.sleep(random.randint(2, 4))
    human_like_scroll_to_bottom()
    logging.info("Màn hình 12 - Click button tôi đồng ý")
    time.sleep(random.randint(2, 4))
    x = random.randint(677, 985)
    y = random.randint(1742, 1819)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    return True

def step13():
    logging.info("Màn hình 13 - Không tìm thấy bản sao lưu")
    if not find_text_on_screen("Không tìm thấy bản sao lưu", timeout=300):
        return False
    logging.info("Màn hình 13 - Click button tiếp theo")
    time.sleep(random.randint(1, 3))
    x = random.randint(752, 993)
    y = random.randint(1775, 1849)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    time.sleep(random.randint(1, 2))
    if find_text_on_screen("Ngày và giờ", timeout=5):
        logging.info("Màn hình 13 - Ngày và giờ, click tiếp theo")
        time.sleep(random.randint(2, 4))
        x = random.randint(741, 987)
        y = random.randint(1765, 1837)
        duration = random.randint(300, 500)
        subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    else:
        logging.info("❌ Không tìm thấy màn 'Ngày và giờ', bỏ qua bước này.")
    return True

def step14():
    time.sleep(random.randint(2, 4))
    logging.info("Màn hình 14 - Bảo vệ điện thoại của bạn")
    if not find_text_on_screen("Bảo vệ điện thoại của bạn", timeout=300):
        return False
    logging.info("Màn hình 14 - Click không cảm ơn")
    time.sleep(random.randint(2, 4))
    x = random.randint(204, 391)
    y = random.randint(1083, 1090)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    logging.info("Màn hình 14 - Click Bỏ qua")
    time.sleep(random.randint(2, 4))
    x = random.randint(776, 977)
    y = random.randint(1223, 1306)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    logging.info("Màn hình 14 - Click Vẫn Bỏ qua")
    time.sleep(random.randint(2, 4))
    x = random.randint(707, 909)
    y = random.randint(1157, 1169)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    return True

def step15():
    logging.info("Màn hình 15 - Cách mới để nói chuyện với Google")
    if not find_text_on_screen("Cách mới để nói chuyện với Google", timeout=300):
        return False
    logging.info("Màn hình 15 - Click Xem thêm")
    time.sleep(random.randint(2, 4))
    x = random.randint(734, 981)
    y = random.randint(1761, 1829)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    logging.info("Màn hình 15 - Click Bật")
    time.sleep(random.randint(2, 3))
    x = random.randint(776, 981)
    y = random.randint(1761, 1829)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    return True

def step16():
    logging.info("Màn hình 16 - Các dịch vụ của Google")
    if not find_text_on_screen("Các dịch vụ của Google", timeout=300):
        return False
    logging.info("Màn hình 16 - Click Khác")
    time.sleep(random.randint(1, 3))
    x = random.randint(800, 975)
    y = random.randint(1781, 1841)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    logging.info("Màn hình 16 - Click Khác")
    time.sleep(random.randint(1, 3))
    x = random.randint(800, 975)
    y = random.randint(1781, 1841)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    logging.info("Màn hình 16 - Click Khác")
    time.sleep(random.randint(1, 3))
    x = random.randint(800, 975)
    y = random.randint(1781, 1841)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    logging.info("Màn hình 16 - Click Chấp nhận")
    time.sleep(random.randint(1, 3))
    x = random.randint(713, 991)
    y = random.randint(1769, 1845)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    return True

def step17():
    logging.info("Màn hình 17 - Không thể khôi phục")
    if not find_text_on_screen("Không thể khôi phục", timeout=300):
        return False
    logging.info("Màn hình 17 - Click Không thể khôi phục")
    time.sleep(random.randint(1, 3))
    x = random.randint(850, 886)
    y = random.randint(1141, 1151)
    duration = random.randint(300, 500)
    subprocess.call(f"adb shell input swipe {x} {y} {x} {y} {duration}", shell=True)
    return True

def step18():
    logging.info("Màn hình 18 - Đăng nhập Samsung account của bạn")
    if not find_text_on_screen("Đăng nhập\nSamsung account của bạn", timeout=300):
        return False
    return True

def insert_gmail(gmail, password):
    """Thử lưu Gmail và Password vào cơ sở dữ liệu, ghi log nếu thất bại."""
    try:
        conn = psycopg2.connect(
            host="51.79.161.237",
            port=5432,
            database="luck_gmail",
            user="luckotp_tool",
            password="Th@pnhjthj3u"
        )
        with conn:
            with conn.cursor() as cur:
                cur.execute(
                    "INSERT INTO gmails (gmail, password) VALUES (%s, %s)",
                    (gmail, password)
                )
        logging.info(f"✅ Đã lưu Gmail {gmail} vào cơ sở dữ liệu.")
        return True
    except Exception as e:
        logging.error(f"❌ Lỗi khi lưu Gmail {gmail} vào cơ sở dữ liệu: {e}")
        logging.info("Tiếp tục lưu vào thư mục Gmails.")
        return False

def save_gmail_password(gmail, password):
    """Lưu Gmail và Password vào file txt và cơ sở dữ liệu sau khi hoàn thành step 18."""
    if not gmail or not password:
        logging.error(f"❌ Không thể lưu: Gmail hoặc mật khẩu rỗng (gmail: {gmail}, password: {password})")
        return False

    # Thử lưu vào cơ sở dữ liệu
    db_success = insert_gmail(gmail, password)
    
    # Lưu vào file txt
    folder = "Gmails"
    try:
        if not os.path.exists(folder):
            os.makedirs(folder)
        
        # Tạo tên file duy nhất bằng timestamp và số ngẫu nhiên
        now = datetime.datetime.now().strftime("%d%m%Y%H%M%S")
        random_suffix = random.randint(1000, 9999)  # Thêm số ngẫu nhiên để tránh trùng
        filename = os.path.join(folder, f"{now}_{random_suffix}.txt")
        
        # Ghi file với error handling
        with open(filename, "w", encoding="utf-8") as f:
            f.write(f"{gmail}|{password}")
        logging.info(f"✅ Đã lưu Gmail {gmail} vào file {filename}")
        return True
    except (IOError, OSError) as e:
        logging.error(f"❌ Lỗi khi lưu file {filename}: {e}")
        return False

def main(wifiname, wifipass, url_proxy):
    serial_no = generate_random_serial()
    mac_address = generate_random_mac()
    logging.info("Đang khởi động lại vào chế độ recovery...")
    run_adb_command(f"adb reboot recovery")
    time.sleep(5)

    data_commands = [
        f"adb shell twrp wipe cache",
        f"adb shell twrp format data",     
        f"adb push SR3-SuperSU-v2.82-SR3.zip /sdcard/",
        f"adb shell twrp install /sdcard/SR3-SuperSU-v2.82-SR3.zip",    
        f"adb shell mount /efs",
        f"adb shell \"mkdir -p /data/misc/adb/\"",     
        f"adb shell \"echo \"{serial_no}\" > /efs/FactoryApp/serial_no\"",
        f"adb shell \"echo \"{mac_address}\" > /efs/wifi/.mac.cob\"",
        f"adb shell \"echo \"{mac_address}\" > /efs/wifi/.mac.info\"",
        f"adb shell \"echo \"QAAAAAHiyjz/4UZVndQCHEE9sZF1BiEHR/AdWS6pj/oyqqQf8T+nRvZG1ZNIy2+JM/MVOs271SK4XwD2JsJulTxQ7uzvK4MvzEXJFnFGeFNmBsMHa8So8baPmyvaotT4HlqTFXUnsaSdYIksEIoyKMWH/Z6MHHqcG8mjOjZucyMUKYh/rfAhU2gLwFloTV6L1xd7Nu27W7U0OYWVXSuZyBWVShf+fv3MoOjQQnVHarcLyfVGyLvGAiKiPMs4EOU0hSsVzP+vEE6EGSlD3EplHo5frW+pgQYMic/9GIke9wgaaaLpbnRtDlWhsBRzPJzO9GeUVRP9yS3lCBmFy++oKQpLUEkxABLu/0CIG9TVOHMnb3xF6XsPROdyw7UVp+P1GJiiXXXnOYwFvE1LrjdGYQ5cGvJkA7YGxImTunMfoFXVsxsVSCjo/SYB/wf1DdPJi6KFC5wxMo9+ttvDY5GI/OR6VrBhBcVlAXAKduvTXDI5WtStEF0yJTDsjRa4EaYMLABK5e/blydVqN2b5mgpV2xT5JZDBHS68EZaAmRb2+hGMDi0DD+1EWpxFQ3eTIXj9ddxIQZ6Iws+obFWB9zAkROYNe2WQwch06nF+pTWQ5UmIMbbjXz/w47DklSOm3D4hZvcImkARMSWztVuXv+tIosSDCiAADMmzUM8e9DPUFdNJb/gHU9MwAEAAQA= unknown@unknown\" > /data/misc/adb/adb_keys\"",
        f"adb shell \"echo \"QAAAADUShenjzygpDNNKOIlN1bJYrdanZ9ET4X74WgsP7VcpnlFTfGHqgMTnSHkLeMNst4O1iVw5xCc58e2AeId99DgzOH3Tfm2TzGsTD2VBarh4fvV3tPgHoKVY62jwzknj2EOrA1td1wD+Hn7w2Epiait8LL/pSP9HS/BCh2YF6jsPM02iCQj0DuG7NIZNjyxRUu6mUaxvoX8OJXVZnSQiPCS1l2qzH7PsAdHslzS737CnOBV65idR1qZ6ZgA8tYI7DD2edbjDPwGUnHearfJQn5J194g4NHPFtX2o2J0+NWMvzApV395JFFfb04oGrQUTUy6z8j1waiBK6azyXlnrY+vRDrDimAwSjoE4ODjrkXa36SrNJlt1xl2XZNw2A5Z36Rh/Ua+LYcqNir6osxzz/oGy/wAMB9gAQ8k8ixlyEsaaCqFoy8Css80cprKkG5KqcPUDtlig4ZPKuwD70ioX9U1vXiuWcFKfSxkFJfoewfDMJjs5rM+VP6Cq3bF+/Iih14nQpNuG5V2SD8qmhsaTbexlXXd7+L/9iaD09uHGvLyka17Hj2BNTcKkITgrYXrzgpMGZgTQ0Fh2nVeOge1HANjw7ecI9+wyNBgdeRvCjw7bwcZJVXROf3Tjd9D43lj17Tn7aedBDUIDXhQ2R7GDGmW6ZURWLKRA75IIh5nEqMfd18cgjgEAAQA= unknown@unknown\" >> /data/misc/adb/adb_keys\"",
        f"adb reboot",
    ]
    
    # Thực thi các lệnh với thời gian chờ (sleep)
    for cmd in data_commands:
        run_adb_command(cmd)
        time.sleep(3)  # Thêm thời gian chờ sau mỗi lệnh

    logging.info(f"Thay đổi IP proxy...")
    try:
        requests.get(url_proxy, timeout=20)
    except:
        pass

    find_text_on_screen("Bắt đầu", True, timeout=300)     
    time.sleep(2)
    subprocess.Popen(f'adb shell su -c whoami', shell=True)
    time.sleep(3)
    subprocess.Popen(f'adb shell input tap 763 1363', shell=True)

    time.sleep(2)
    subprocess.Popen(f'adb shell su -c whoami', shell=True)
    time.sleep(3)
    subprocess.Popen(f'adb shell input tap 763 1363', shell=True)
    
    time.sleep(2)

    subprocess.Popen(f'adb shell su -c whoami', shell=True)
    time.sleep(3)
    subprocess.Popen(f'adb shell input tap 763 1363', shell=True)
    time.sleep(2)

    while True:
        try:
            if get_window_coordinates_and_focus():
                for _ in range(3):
                    logging.info("Bước 1: Click vào tab Repair (450, 140)")
                    click_at_coordinates(450, 140)
                    time.sleep(2)
                    logging.info("Bước 2: Click vào dropdown để chọn thiết bị (635, 185)")
                    click_at_coordinates(635, 185)
                    time.sleep(2)
                    logging.info("Bước 3: Nhấn pagedown và enter")
                    pyautogui.press('pagedown')
                    time.sleep(2)
                    pyautogui.press('enter')
                logging.info("Bước 4: Sinh số IMEI")
                random_numbers = generate_imei()
                time.sleep(2)
                logging.info("Bước 5: Click vào (361, 236) và nhập IMEI")
                click_at_coordinates(361, 236)
                for num in random_numbers:
                    pyautogui.press(str(num))
                logging.info(f"Đã nhập IMEI: {random_numbers}")
                logging.info("Bước 6: Sinh số IMEI2")
                random_numbers1 = generate_imei()
                time.sleep(2)
                logging.info("Bước 7: Click vào (361, 264) và nhập IMEI2")
                click_at_coordinates(361, 264)
                for num in random_numbers1:
                    pyautogui.press(str(num))
                logging.info(f"Đã nhập IMEI2: {random_numbers1}")
                time.sleep(2)
                logging.info("Bước 8: Click vào button Repair (740, 240)")
                click_at_coordinates(740, 240)
                time.sleep(60)
                logging.info("Bước 9: Click loại bỏ tích ở IMEI và IMEI2 (361, 236) và (361, 264)")
                click_at_coordinates(361, 236)
                time.sleep(1)
                click_at_coordinates(361, 264)
                break
            else:
                logging.info("Không thể lấy tọa độ cửa sổ, dừng chương trình.")
                return
        except Exception as e:
            logging.error(f"❌ Lỗi trong quá trình xử lý Z3X: {e}")
            return

    run_adb_command(f"adb reboot recovery")
    time.sleep(3)  # Đợi 3 giây sau lệnh reboot recovery
    commands = [
        f"adb shell rm /data/misc/wifi/WifiConfigStore.xml",
        f"adb push WifiConfigStore.xml /data/misc/wifi/",
        f"adb shell \"sed -i 's/team37/{wifiname}/g' /data/misc/wifi/WifiConfigStore.xml\"",
        f"adb shell \"sed -i 's/1234AbCd/{wifipass}/g' /data/misc/wifi/WifiConfigStore.xml\"",
        f"adb push UPDATE-unSU-signed.zip /sdcard/",
        f"adb shell twrp install /sdcard/UPDATE-unSU-signed.zip",
        f"adb reboot"
    ]
    for cmd in commands:
        run_adb_command(cmd)
        time.sleep(3)  # Đợi 3 giây sau mỗi lệnh

    # Khởi tạo biến để lưu Gmail và password
    gmail = None
    password = None

    # Thực hiện các bước từ 1 đến 18
    if not step1():
        logging.warning("⚠ Step 1 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    if not step2():
        logging.warning("⚠ Step 2 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    if not step3():
        logging.warning("⚠ Step 3 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    if not step4():
        logging.warning("⚠ Step 4 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    if not step5():
        logging.warning("⚠ Step 5 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    if not step6():
        logging.warning("⚠ Step 6 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    if not step7():
        logging.warning("⚠ Step 7 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    gmail = step8()
    if not gmail:
        logging.warning("⚠ Step 8 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    password = step9()
    if not password:
        logging.warning("⚠ Step 9 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    if not step10():
        logging.warning("⚠ Step 10 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    if not step11():
        logging.warning("⚠ Step 11 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    if not step12():
        logging.warning("⚠ Step 12 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    if not step13():
        logging.warning("⚠ Step 13 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    if not step14():
        logging.warning("⚠ Step 14 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    if not step15():
        logging.warning("⚠ Step 15 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    if not step16():
        logging.warning("⚠ Step 16 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    if not step17():
        logging.warning("⚠ Step 17 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return
    if not step18():
        logging.warning("⚠ Step 18 thất bại, không lưu Gmail và tiếp tục lần chạy tiếp theo")
        return

    # Chỉ lưu Gmail và password sau khi hoàn thành step 18
    save_gmail_password(gmail, password)

with open("config.txt", "r", encoding="utf-8") as f:
    lines = f.read().splitlines()
    wifiname = lines[0]
    wifipass = lines[1]
    url_proxy = lines[2]

try:
    run_count = 0
    while True:
        if os.path.exists("ui.xml"):
            os.remove("ui.xml")
        if os.path.exists("screen_before.png"):
            os.remove("screen_before.png")
        if os.path.exists("screen_after.png"):
            os.remove("screen_after.png")
        logging.info(f"🔁 Lần chạy thứ {run_count + 1}")
        try:
            main(wifiname, wifipass, url_proxy)
        except Exception as e:
            logging.error(f"❌ Lỗi trong lần chạy {run_count + 1}: {e}")
            logging.info("Tiếp tục lần chạy tiếp theo...")
        run_count += 1
        if run_count >= 1000:
            logging.info("✅ Đã hoàn thành 1000 lần. Kết thúc chương trình.")
            break
except KeyboardInterrupt:
    logging.info("\n⛔ Đã nhận lệnh dừng từ người dùng (Ctrl+C). Kết thúc chương trình.")
    # Hoan thien ver z1



    
    