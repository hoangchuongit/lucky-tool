import random
import time
import pyautogui
import subprocess
import pygetwindow as gw
import random
import os
import requests
import xml.etree.ElementTree as ET
import threading
import hashlib
import psycopg2
from faker import Faker
import datetime
fake = Faker('en_US')


def generate_random_serial():
    """Tao so serial ngau nhien theo dinh dang R**A**KK**."""
    return f"R{random.randint(1111111111, 9999999999)}"


def generate_imei():
    imei_prefix = "356406"
    random_numbers = [random.randint(0, 9) for _ in range(6)]
    imei_body = imei_prefix + ''.join(map(str, random_numbers))

    def calculate_luhn(imei):
        digits = [int(d) for d in imei]
        for i in range(len(digits) - 1, -1, -2):
            digits[i] *= 2
            if digits[i] > 9:
                digits[i] -= 9
        total_sum = sum(digits)
        return (10 - (total_sum % 10)) % 10
    check_digit = calculate_luhn(imei_body + "0")
    full_imei = imei_body + str(check_digit)
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
    """Kiểm tra xem có thiết bị nào kết nối qua adb không."""
    try:
        output = subprocess.check_output(
            "adb devices", shell=True).decode('utf-8')
        lines = output.strip().split('\n')
        devices = [
            line for line in lines[1:]
            if line.strip() and (line.strip().endswith('\tdevice') or line.strip().endswith('\trecovery'))
        ]
        return len(devices) > 0
    except Exception:
        return False


def restart_adb_server():
    """Kill và restart adb server."""
    subprocess.call("adb kill-server", shell=True)
    time.sleep(1)
    subprocess.call("adb start-server", shell=True)
    time.sleep(1)


def run_adb_command(command):
    """
    Chạy lệnh ADB nếu có thiết bị. Thử lại tối đa 30 lần.
    Nếu sau 30 lần không có thiết bị thì restart adb server và thử lại 1 lần cuối.
    """
    while True:
        retries = 0
        while retries <= 30:
            if check_adb_devices():
                break
            print(f"Chua ket noi duoc thiet bi, thu lai lan {retries}/30...")
            retries += 1
            time.sleep(5)
        if retries == 30:
            print("Khong tim thay thiet bi sau 30 lan thu. Dang restart adb server...")
            restart_adb_server()
        else:
            break

    # Đã tìm thấy thiết bị, chạy lệnh
    try:
        print(f"Chay lenh: {command}")
        return subprocess.check_output(command, shell=True, stderr=subprocess.STDOUT).decode('utf-8')
    except subprocess.CalledProcessError as e:
        return e.output.decode('utf-8')


def get_window_coordinates_and_focus():
    global win_x, win_y
    print("Dang tim cua so Z3X Samsung Tool PRO...")
    try:
        windows = [win for win in gw.getAllWindows() if win.title.strip()]
        target_window = next(
            (win for win in windows if 'z3x samsung tool pro' in win.title.lower()), None)
        if not target_window:
            print("Khong tim thay cua so Z3X Samsung Tool PRO")
            return False
        print("Da tim thay cua so, dang focus...")
        if not target_window.isActive:
            target_window.activate()
            print("Da focus vao cua so.")
            time.sleep(2)
        if not target_window.isMaximized:
            print("Dang phong to (maximize) cua so...")
            target_window.maximize()
            time.sleep(2)
        return True
    except Exception as e:
        print(f"Da xay ra loi khi lay toa do/focus cua so: {e}")
        return False


def click_at_coordinates(x, y):
    print(f"Di chuyen chuot toi ({x},{y})")
    try:
        pyautogui.click(x, y, duration=0.5)
        print(f"Da toi vi tri ({x}, {y}), click!")
        time.sleep(2)
    except Exception as e:
        print(f"Da xay ra loi khi click: {e}")


def find_text_on_screen(target_text="Bắt đầu", on_screen=False, timeout=None):
    count = 0
    while True:
        try:
            # Làm sáng màn hình thì mới lấy text được
            if on_screen:
                run_adb_command("adb shell input keyevent 224")
            # Dump UI hierarchy ra file xml trên thiết bị
            run_adb_command("adb shell uiautomator dump /sdcard/ui.xml")
            # Pull file xml về máy tính
            run_adb_command("adb pull /sdcard/ui.xml")
            # Parse file xml
            tree = ET.parse("ui.xml")
            root = tree.getroot()
            for node in root.iter('node'):
                text = node.attrib.get('text')
                if text and target_text.lower() in text.lower():
                    print(f'Tim thay: "{text}"')
                    os.remove("ui.xml")
                    return True
        except:
            if os.path.exists("ui.xml"):
                os.remove("ui.xml")
            print(f'Khong tim thay "{target_text}" tren man hinh. Thu lai sau 5s.')
            time.sleep(5)
        count += 1
        if timeout is not None and count >= timeout:
            print(f"Đã thử {timeout} lần nhưng không tìm thấy '{target_text}'.")
            return False


def extract_first_gmail():
    while True:
        try:
            # Làm sáng màn hình thì mới lấy text được
            run_adb_command("adb shell input keyevent 224")
            # Dump UI hierarchy ra file xml trên thiết bị
            run_adb_command("adb shell uiautomator dump /sdcard/ui.xml")
            # Pull file xml về máy tính
            run_adb_command("adb pull /sdcard/ui.xml")
            # Parse file xml
            tree = ET.parse("ui.xml")
            root = tree.getroot()
            for node in root.iter('node'):
                text = node.attrib.get('text', '')
                if '@gmail.com' in text:
                    print(f'Tim thay gmail: "{text}"')
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
        print(f"Loi khi thuc hien tap: {str(e)}")


def input_text_with_delay(text, min_delay=0.1, max_delay=0.3):
    """Nhập văn bản với độ trễ ngẫu nhiên giữa các ký tự"""
    try:
        for char in text:
            escaped_char = char.replace('"', '\\"').replace('\\', '\\\\')
            # run_adb_command(f'adb shell input text "{escaped_char}"')
            subprocess.Popen(f'adb shell input text "{escaped_char}"')
            time.sleep(random.uniform(min_delay, max_delay))
    except Exception as e:
        print(f"Lỗi khi nhập text: {str(e)}", "ERROR")


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


def human_like_scroll_to_bottom(max_scrolls=10):
    last_md5 = None
    for i in range(max_scrolls):
        screenshot_pull("screen_before.png")
        before_md5 = file_md5("screen_before.png")
        # Nếu giống MD5 với lần trước thì đã ở đáy, dừng luôn
        if before_md5 == last_md5:
            print("Da scroll den cuoi noi dung (anh man hinh khong doi).")
            return
        last_md5 = before_md5

        # Thực hiện scroll giống người
        x = random.randint(520, 560)
        y1 = random.randint(1600, 1750)
        y2 = random.randint(350, 600)
        duration = random.randint(300, 700)
        cmd = f"adb shell input swipe {x} {y1} {x + random.randint(-8,8)} {y2} {duration}"
        print(f"Scroll lan {i+1}: {cmd}")
        subprocess.call(cmd, shell=True)
        time.sleep(random.uniform(0.5, 1.2))

        # Chờ màn hình ổn định rồi chụp lại ảnh
        time.sleep(1)
        screenshot_pull("screen_after.png")
        after_md5 = file_md5("screen_after.png")

        if before_md5 == after_md5:
            print("Da scroll den cuoi noi dung (anh man hinh khong doi).")
            return

    print("Da scroll het so lan quy dinh (co the chua toi day).")


def step1():
    print("Man hinh 1 - Chao mung")
    find_text_on_screen("Bắt đầu", True)
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(510, 563), y_range=(772, 837))


def step2():
    print("Man hinh 2 - Ket noi wifi")
    find_text_on_screen("Tiếp theo")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(752, 886), y_range=(1833, 1849))


def step3():
    print("Man hinh 3 - Dong y voi cac dieu khoan dieu kien")
    find_text_on_screen("Đồng ý với các Điều khoản và Điều kiện")
    time.sleep(random.randint(2, 5))
    print("Man hinh 3 - click de scroll xuong")
    random_input_swipe(x_range=(950, 1000), y_range=(1818, 1890))
    time.sleep(random.randint(2, 5))
    print("Man hinh 3 - Click toi da doc va dong y voi tat ca noi dung tren")
    random_input_swipe(x_range=(112, 134), y_range=(1630, 1654))
    time.sleep(random.randint(2, 5))
    print("Man hinh 3 - Click nut tiep theo")
    random_input_swipe(x_range=(748, 903), y_range=(1837, 1851))


def step4():
    print("Man hinh 4 - Tao tai khoan gmail")
    find_text_on_screen("Đăng nhập")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(102, 309), y_range=(1453, 1463))


def step5():
    print("Man hinh 5 - Danh cho muc dich ca nhan cua toi")
    find_text_on_screen("Dành cho mục đích cá nhân của tôi")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(118, 677), y_range=(1018, 1024))


def step6():
    print("Man hinh 6 - Tao tai khoan Google")
    find_text_on_screen("Tạo Tài khoản Google")
    time.sleep(random.randint(2, 5))
    name = fake.name()
    parts = name.split()
    print("Man hinh 6 - Nhap Ho")
    first_name = parts[0]
    random_input_swipe(x_range=(126, 754), y_range=(682, 784))
    time.sleep(random.randint(2, 5))
    input_text_with_delay(first_name)
    time.sleep(random.randint(2, 5))
    print("Man hinh 6 - Nhap Ten")
    last_name = parts[-1]
    random_input_swipe(x_range=(118, 677), y_range=(1018, 1024))
    time.sleep(random.randint(2, 5))
    input_text_with_delay(last_name)
    time.sleep(random.randint(2, 5))
    print("Man hinh 6 - Click button tiep theo")
    random_input_swipe(x_range=(691, 985), y_range=(873, 953))


def step7():
    print("Man hinh 7 - Thong tin co ban")
    find_text_on_screen("Thông tin cơ bản")
    time.sleep(random.randint(2, 5))

    print("Man hinh 7 - Nhap ngay sinh")
    day = random.randint(1, 28)
    random_input_swipe(x_range=(138, 315), y_range=(704, 770))
    time.sleep(random.randint(2, 5))
    input_text_with_delay(str(day))

    print("Man hinh 7 - Nhap thang sinh")
    month = random.randint(1, 7)
    random_input_swipe(x_range=(452, 573), y_range=(696, 760))
    time.sleep(random.randint(2, 5))

    thang_coords = [
        [(466, 593), (895, 915)],     # Tháng 1
        [(460, 597), (1038, 1062)],   # Tháng 2
        [(462, 613), (1187, 1213)],   # Tháng 3
        [(454, 611), (1322, 1340)],   # Tháng 4
        [(460, 607), (1469, 1483)],   # Tháng 5
        [(458, 609), (1606, 1632)],   # Tháng 6
        [(458, 601), (1754, 1775)],   # Tháng 7
    ]
    coords = thang_coords[month - 1]
    random_input_swipe(x_range=coords[0], y_range=coords[1])
    time.sleep(random.randint(2, 5))

    print("Man hinh 7 - Nhap nam sinh")
    dob = fake.date_of_birth(minimum_age=18, maximum_age=45)
    year = dob.year
    random_input_swipe(x_range=(573, 907), y_range=(704, 770))
    time.sleep(random.randint(2, 5))
    input_text_with_delay(str(year))

    print("Man hinh 7 - Nhap gioi tinh")
    gender = random.randint(1, 2)
    random_input_swipe(x_range=(130, 323), y_range=(917, 972))
    time.sleep(random.randint(2, 5))

    gender_coords = [
        [(110, 315), (1096, 1165)],   # Nữ
        [(118, 386), (1247, 1277)]    # Nam
    ]
    coords_gender = gender_coords[gender - 1]
    random_input_swipe(x_range=coords_gender[0], y_range=coords_gender[1])
    time.sleep(random.randint(2, 5))

    print("Man hinh 7 - Click button tiep theo")
    random_input_swipe(x_range=(685, 983), y_range=(1744, 1821))


def step8():
    print("Man hinh 8 - Chon dia chi Gmail cua ban")
    if find_text_on_screen("Chọn địa chỉ Gmail của bạn", False, 5):
        print("Man hinh 8 - Tim: Tao dia chi Gmail cua rieng ban")
        find_text_on_screen("Tạo địa chỉ Gmail của riêng bạn")
        time.sleep(random.randint(2, 5))
        print("Man hinh 8 - Chon gmail dau tien")
        random_input_swipe(x_range=(200, 800), y_range=(747, 805))
        gmail = extract_first_gmail()
        time.sleep(random.randint(2, 5))

        print("Man hinh 8 - Click button tiep theo")
        random_input_swipe(x_range=(689, 995), y_range=(1750, 1823))

        return gmail
    else:
        print("Man hinh 8 - Tao mot dia chi Gmail de dang nhap vao Tai khoan Google cua ban")
        find_text_on_screen("Tạo một địa chỉ Gmail để đăng nhập vào Tài khoản Google của bạn")
        random_input_swipe(x_range=(104, 677), y_range=(744, 847))
        while True:
            gmail = fake_realistic_gmail()
            time.sleep(random.randint(2, 5))
            input_text_with_delay(gmail)
            time.sleep(random.randint(2, 5))

            print("Man hinh 8 - An nut BACK")
            run_adb_command("adb shell input keyevent 4")
            time.sleep(random.randint(2, 5))

            print("Man hinh 8 - Click button tiep theo")
            random_input_swipe(x_range=(693, 993), y_range=(1748, 1819))

            if find_text_on_screen("Tên người dùng đã được sử dụng", False, 3):
                random_input_swipe(x_range=(104, 677), y_range=(744, 847))
                # di chuyển con trỏ xuống cuối
                run_adb_command("adb shell input keyevent 123")
                # xóa gmail cũ
                for _ in len(gmail):
                    run_adb_command("adb shell input keyevent 67")
            else:
                break

        return f"{gmail}@gmail.com"


def step9():
    print("Man hinh 9 - Tao mot mat khau manh")
    find_text_on_screen("Tạo một mật khẩu mạnh")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(136, 677), y_range=(748, 855))
    
    print("Man hinh 9 - Tao mot mat khau manh")
    while True:
        password = fake.password(
            length=random.randint(10, 14),
            special_chars=True,
            digits=True,
            upper_case=True,
            lower_case=True
        )
        if "|" not in password:
            break

    time.sleep(random.randint(2, 5))
    input_text_with_delay(password)

    print("Man hinh 9 - Ấn nút BACK")
    run_adb_command("adb shell input keyevent 4")
    time.sleep(random.randint(2, 5))

    print("Man hinh 9 - Click button tiep theo")
    random_input_swipe(x_range=(689, 979), y_range=(1744, 1819))

    return password


def step10():
    if find_text_on_screen("Xác minh bạn không phải là ro-bot", False, 3):
        return False
    
    print("Man hinh 10 - Thêm số điện thoại")
    find_text_on_screen("Thêm số điện thoại")
    time.sleep(random.randint(2, 5))
    human_like_scroll_to_bottom()

    print("Man hinh 10 - Click lựa chọn khác")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(86, 357), y_range=(1531, 1553))

    print("Man hinh 10 - Chọn tuỳ chọn phù hợp với bạn")
    find_text_on_screen("Chọn tuỳ chọn phù hợp với bạn")
    time.sleep(random.randint(2, 5))

    print("Man hinh 10 - Không, đừng thêm số điện thoại")
    random_input_swipe(x_range=(96, 114), y_range=(688, 708))
    time.sleep(random.randint(2, 5))
    human_like_scroll_to_bottom()

    print("Man hinh 10 - Click button xong")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(774, 989), y_range=(1744, 1819))


def step11():
    print("Man hinh 11 - Xem lại thông tin tài khoản của bạn")
    find_text_on_screen("Xem lại thông tin tài khoản của bạn")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(703, 991), y_range=(1750, 1823))

def step12():
    print("Man hinh 12 - Quyền riêng tư và điều khoản")
    find_text_on_screen("Quyền riêng tư và điều khoản")
    time.sleep(random.randint(2, 5))
    human_like_scroll_to_bottom()

    print("Man hinh 10 - Click button tôi đồng ý")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(677, 985), y_range=(1742, 1819))

def step13():
    print("Man hinh 13 - Không tìm thấy bản sao lưu")
    find_text_on_screen("Không tìm thấy bản sao lưu")
    print("Man hinh 13 - Click button tiếp theo")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(752, 993), y_range=(1775, 1849))

def step14():
    print("Man hinh 14 - Bảo vệ điện thoại của bạn")
    find_text_on_screen("Bảo vệ điện thoại của bạn")
    print("Man hinh 14 - Click không cảm ơn")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(78, 100), y_range=(1070, 1084))

    print("Man hinh 14 - Click Bỏ qua")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(776, 977), y_range=(1223, 1306))

    print("Man hinh 14 - Click Vẫn Bỏ qua")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(707, 909), y_range=(1157, 1169))

def step15():
    print("Man hinh 15 - Cách mới để nói chuyện với Google")
    find_text_on_screen("Cách mới để nói chuyện với Google")
    print("Man hinh 15 - Click Xem thêm")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(734, 981), y_range=(1761, 1829))

    print("Man hinh 15 - Click Bật")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(776, 981), y_range=(1761, 1829))

def step16():
    print("Man hinh 16 - Cách dịch vụ của Google")
    find_text_on_screen("Cách dịch vụ của Google")
    print("Man hinh 16 - Click Khác")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(780, 985), y_range=(1777, 1841))
    print("Man hinh 16 - Click Khác")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(780, 985), y_range=(1777, 1841))
    print("Man hinh 16 - Click Khác")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(780, 985), y_range=(1777, 1841))

    print("Man hinh 16 - Click Chấp nhận")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(713, 991), y_range=(1769, 1845))

def step17():
    print("Man hinh 17 - Không thể khôi phục")
    find_text_on_screen("Không thể khôi phục")
    print("Man hinh 17 - Click Không thể khôi phục")
    time.sleep(random.randint(2, 5))
    random_input_swipe(x_range=(850, 886), y_range=(1141, 1151))

def step18():
    print("Man hinh 18 - Đăng nhập Samsung account của bạn")
    find_text_on_screen("Đăng nhập Samsung account của bạn")


def save_gmail_password(gmail, password):
    """Luu Gmail va Password vao file txt dinh dang DDMMYYYYHHmmss.txt trong folder Gmails. Neu chua co folder thi tao moi."""
    insert_gmail(gmail, password)
    folder = "Gmails"
    if not os.path.exists(folder):
        os.makedirs(folder)
    now = datetime.datetime.now().strftime("%d%m%Y%H%M%S")
    filename = os.path.join(folder, f"{now}.txt")
    with open(filename, "w", encoding="utf-8") as f:
        f.write(f"{gmail}|{password}")

def insert_gmail(gmail, password):
    conn = psycopg2.connect(
        host="51.79.161.237",
        port=5432,
        database="luck_gmail",
        user="luckotp_tool",
        password="Th@pnhjthj3u"
    )
    try:
        with conn:
            with conn.cursor() as cur:
                cur.execute(
                    "INSERT INTO gmails (gmail, password) VALUES (%s, %s)", 
                    (gmail, password)
                )
    finally:
        conn.close()

def main(wifiname, wifipass, url_proxy):
    # Địa chỉ serial no mới
    serial_no = generate_random_serial()

    # Địa chỉ MAC mới
    mac_address = generate_random_mac()

    # Khởi động máy vào chế đọ Recovery
    print("Dang khoi dong lai vao che do recovery...")
    run_adb_command(f"adb reboot recovery")

    # Tạo một thread để đổi proxy sau 10 giây
    def delayed_proxy():
        time.sleep(10)
        print(f"Thay doi IP proxy...")
        try:
            requests.get(url_proxy, timeout=20)
        except:
            pass

    proxy_thread = threading.Thread(target=delayed_proxy)
    proxy_thread.start()

    # Thực hiện root máy với SuperSu ở trong twrp
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
    for cmd in data_commands:
        run_adb_command(cmd)

    # Chờ đến khi có text Bắt đầu trên màn hình
    find_text_on_screen("Bắt đầu")

    # Cấp quyền su để ng
    subprocess.Popen(f'adb shell su -c whoami', shell=True)
    time.sleep(5)

    subprocess.Popen(f'adb shell input tap 763 1363', shell=True)
    time.sleep(2)

    # def thread_accept_root():
    #     if find_text_on_screen("Yêu cầu quyền truy câp superuser", False, 30):
    #         run_adb_command(f'adb shell input tap 763 1363')

    # accept_root_thread = threading.Thread(target=thread_accept_root)
    # accept_root_thread.start()

    while True:
        try:
            if get_window_coordinates_and_focus():
                for i in range(2):
                    print("Buoc 1: Click vao tab Repair (350, 32)")
                    click_at_coordinates(350, 32)
                    # time.sleep(3)

                    print("Buoc 2: Click vao dropdown de chon thiet bi (843, 75)")
                    click_at_coordinates(843, 75)
                    # time.sleep(3)

                    print("Buoc 3: Nhan pagedown va enter")
                    pyautogui.press('pagedown')
                    time.sleep(2)
                    pyautogui.press('enter')

                print("Buoc 4: Sinh so IMEI")
                random_numbers = generate_imei()

                print("Buoc 5: Click vao (253, 128) va nhap IMEI")
                click_at_coordinates(253, 128)
                for num in random_numbers:
                    pyautogui.press(str(num))
                print(f"Da nhap IMEI: {random_numbers}")

                print("Buoc 6: Sinh so IMEI2")
                random_numbers1 = generate_imei()

                print("Buoc 7: Click vao (253, 155) va nhap IMEI2")
                click_at_coordinates(253, 155)
                for num in random_numbers1:
                    pyautogui.press(str(num))
                print(f"Da nhap IMEI2: {random_numbers1}")

                print("Buoc 8: Click vao button Repair (1000, 130)")
                click_at_coordinates(1000, 130)
                time.sleep(60)

                print("Buoc 9: Click loai bo tich o IMEI va IMEI2 (253, 128) va (253, 155)")
                click_at_coordinates(253, 128)
                click_at_coordinates(253, 155)

                break
            else:
                print("Khong the lay toa do cua so, dung chuong trinh.")
        except:
            pass

    run_adb_command(f"adb reboot recovery")

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

    # Bắt đầu reg gmail
    step1()

    step2()

    step3()

    step4()

    step5()

    step6()

    step7()

    gmail = step8()

    password = step9()

    isDone = step10()
    if isDone==False:
        return

    step11()

    step12()

    step13()

    step14()

    step15()

    step16()

    step17()

    step18()

    save_gmail_password(gmail, password)


with open("config.txt", "r", encoding="utf-8") as f:
    lines = f.read().splitlines()
    wifiname = lines[0]
    wifipass = lines[1]
    url_proxy = lines[2]

try:
    # while True:
    if os.path.exists("ui.xml"):
        os.remove("ui.xml")

    main(wifiname, wifipass, url_proxy)
    # main("P1632", "Th@pnhjthj3u", url_proxy)

except KeyboardInterrupt:
    print("\nDa nhan lenh dung tu nguoi dung (Ctrl+C). Ket thuc chuong trinh.")
