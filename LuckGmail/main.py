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
from faker import Faker
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
        output = subprocess.check_output("adb devices", shell=True).decode('utf-8')
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
        while retries<=30:
            if check_adb_devices():
                break
            print(f"Chua ket noi duoc thiet bi, thu lai lan {retries}/30...")
            retries+=1
            time.sleep(5)
        if retries==30:
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
        target_window = next((win for win in windows if 'z3x samsung tool pro' in win.title.lower()), None)
        if not target_window:
            print("Khong tim thay cua so Z3X Samsung Tool PRO")
            return False
        print("Da tim thay cua so, dang focus...")
        if not target_window.isActive:
            target_window.activate()
            print("Da focus vao cua so.")
            time.sleep(1)
        if not target_window.isMaximized:
            print("Dang phong to (maximize) cua so...")
            target_window.maximize()
            time.sleep(1)
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
        
def find_text_on_screen(target_text="Bắt đầu"):
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
                text = node.attrib.get('text')
                if text and target_text.lower() in text.lower():
                    print(f'Da tim thay: "{text}"')
                    os.remove("ui.xml")
                    return
        except:
            if os.path.exists("ui.xml"):
                os.remove("ui.xml")
            print(f'Khong tim thay "{target_text}" tren man hinh. Thu lai sau 5s.')
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
            run_adb_command(f'adb shell input text "{escaped_char}"')
            time.sleep(random.uniform(min_delay, max_delay))
    except Exception as e:
        print(f"Lỗi khi nhập text: {str(e)}", "ERROR")

def main(wifiname, wifipass, url_proxy):
    # # Địa chỉ serial no mới
    # serial_no = generate_random_serial()

    # # Địa chỉ MAC mới
    # mac_address = generate_random_mac()
    
    # # Khởi động máy vào chế đọ Recovery
    # print("Dang khoi dong lai vao che do recovery...")
    # run_adb_command(f"adb reboot recovery")

    # # Tạo một thread để đổi proxy sau 10 giây
    # def delayed_proxy():
    #     time.sleep(10)
    #     print(f"Thay doi IP proxy...")
    #     try:
    #         requests.get(url_proxy, timeout=20)
    #     except:
    #         pass

    # proxy_thread = threading.Thread(target=delayed_proxy)
    # proxy_thread.start()

    # # Thực hiện root máy với SuperSu ở trong twrp
    # data_commands = [
    #     f"adb shell twrp wipe cache",
    #     f"adb shell twrp format data",
    #     f"adb push SR3-SuperSU-v2.82-SR3.zip /sdcard/",
    #     f"adb shell twrp install /sdcard/SR3-SuperSU-v2.82-SR3.zip",
    #     f"adb shell mount /efs",
    #     f"adb shell \"mkdir -p /data/misc/adb/\"",
    #     f"adb shell \"echo \"{serial_no}\" > /efs/FactoryApp/serial_no\"",
    #     f"adb shell \"echo \"{mac_address}\" > /efs/wifi/.mac.cob\"",
    #     f"adb shell \"echo \"{mac_address}\" > /efs/wifi/.mac.info\"",
    #     f"adb shell \"echo \"QAAAAAHiyjz/4UZVndQCHEE9sZF1BiEHR/AdWS6pj/oyqqQf8T+nRvZG1ZNIy2+JM/MVOs271SK4XwD2JsJulTxQ7uzvK4MvzEXJFnFGeFNmBsMHa8So8baPmyvaotT4HlqTFXUnsaSdYIksEIoyKMWH/Z6MHHqcG8mjOjZucyMUKYh/rfAhU2gLwFloTV6L1xd7Nu27W7U0OYWVXSuZyBWVShf+fv3MoOjQQnVHarcLyfVGyLvGAiKiPMs4EOU0hSsVzP+vEE6EGSlD3EplHo5frW+pgQYMic/9GIke9wgaaaLpbnRtDlWhsBRzPJzO9GeUVRP9yS3lCBmFy++oKQpLUEkxABLu/0CIG9TVOHMnb3xF6XsPROdyw7UVp+P1GJiiXXXnOYwFvE1LrjdGYQ5cGvJkA7YGxImTunMfoFXVsxsVSCjo/SYB/wf1DdPJi6KFC5wxMo9+ttvDY5GI/OR6VrBhBcVlAXAKduvTXDI5WtStEF0yJTDsjRa4EaYMLABK5e/blydVqN2b5mgpV2xT5JZDBHS68EZaAmRb2+hGMDi0DD+1EWpxFQ3eTIXj9ddxIQZ6Iws+obFWB9zAkROYNe2WQwch06nF+pTWQ5UmIMbbjXz/w47DklSOm3D4hZvcImkARMSWztVuXv+tIosSDCiAADMmzUM8e9DPUFdNJb/gHU9MwAEAAQA= unknown@unknown\" > /data/misc/adb/adb_keys\"",
    #     f"adb shell \"echo \"QAAAADUShenjzygpDNNKOIlN1bJYrdanZ9ET4X74WgsP7VcpnlFTfGHqgMTnSHkLeMNst4O1iVw5xCc58e2AeId99DgzOH3Tfm2TzGsTD2VBarh4fvV3tPgHoKVY62jwzknj2EOrA1td1wD+Hn7w2Epiait8LL/pSP9HS/BCh2YF6jsPM02iCQj0DuG7NIZNjyxRUu6mUaxvoX8OJXVZnSQiPCS1l2qzH7PsAdHslzS737CnOBV65idR1qZ6ZgA8tYI7DD2edbjDPwGUnHearfJQn5J194g4NHPFtX2o2J0+NWMvzApV395JFFfb04oGrQUTUy6z8j1waiBK6azyXlnrY+vRDrDimAwSjoE4ODjrkXa36SrNJlt1xl2XZNw2A5Z36Rh/Ua+LYcqNir6osxzz/oGy/wAMB9gAQ8k8ixlyEsaaCqFoy8Css80cprKkG5KqcPUDtlig4ZPKuwD70ioX9U1vXiuWcFKfSxkFJfoewfDMJjs5rM+VP6Cq3bF+/Iih14nQpNuG5V2SD8qmhsaTbexlXXd7+L/9iaD09uHGvLyka17Hj2BNTcKkITgrYXrzgpMGZgTQ0Fh2nVeOge1HANjw7ecI9+wyNBgdeRvCjw7bwcZJVXROf3Tjd9D43lj17Tn7aedBDUIDXhQ2R7GDGmW6ZURWLKRA75IIh5nEqMfd18cgjgEAAQA= unknown@unknown\" >> /data/misc/adb/adb_keys\"",
    #     f"adb reboot",
    # ]
    # for cmd in data_commands:
    #     run_adb_command(cmd)

    # # Chờ đến khi có text Bắt đầu trên màn hình
    # find_text_on_screen("Bắt đầu")

    # # Cấp quyền su để ng
    # subprocess.Popen(f'adb shell su -c whoami', shell=True)
    # time.sleep(2)

    # subprocess.Popen(f'adb shell input tap 763 1363', shell=True)
    # time.sleep(2)

    # while True:
    #     try:
    #         if get_window_coordinates_and_focus():
    #             for i in range(2):
    #                 print("Buoc 1: Click vao tab Repair (350, 32)")
    #                 click_at_coordinates(350, 32)
    #                 # time.sleep(3)

    #                 print("Buoc 2: Click vao dropdown de chon thiet bi (350, 75)")
    #                 click_at_coordinates(350, 75)
    #                 # time.sleep(3)

    #                 print("Buoc 3: Nhan pagedown va enter")
    #                 pyautogui.press('pagedown')
    #                 time.sleep(2)
    #                 pyautogui.press('enter')

    #             print("Buoc 4: Sinh so IMEI")
    #             random_numbers = generate_imei()

    #             print("Buoc 5: Click vao (253, 128) va nhap IMEI")
    #             click_at_coordinates(253, 128)
    #             for num in random_numbers:
    #                 pyautogui.press(str(num))
    #             print(f"Da nhap IMEI: {random_numbers}")

    #             print("Buoc 6: Sinh so IMEI2")
    #             random_numbers1 = generate_imei()

    #             print("Buoc 7: Click vao (253, 155) va nhap IMEI2")
    #             click_at_coordinates(253, 155)
    #             for num in random_numbers1:
    #                 pyautogui.press(str(num))
    #             print(f"Da nhap IMEI2: {random_numbers1}")

    #             print("Buoc 8: Click vao button Repair (1000, 130)")
    #             click_at_coordinates(1000, 130)
    #             time.sleep(60)

    #             print("Buoc 9: Click loai bo tich o IMEI va IMEI2 (253, 128) va (253, 155)")
    #             click_at_coordinates(253, 128)
    #             click_at_coordinates(253, 155)

    #             break
    #         else:
    #             print("Khong the lay toa do cua so, dung chuong trinh.")
    #     except:
    #         pass
        
    # run_adb_command(f"adb reboot recovery")
    
    # commands = [
    #     f"adb shell rm /data/misc/wifi/WifiConfigStore.xml",
    #     f"adb push WifiConfigStore.xml /data/misc/wifi/",
    #     f"adb shell \"sed -i 's/team37/{wifiname}/g' /data/misc/wifi/WifiConfigStore.xml\"",
    #     f"adb shell \"sed -i 's/1234AbCd/{wifipass}/g' /data/misc/wifi/WifiConfigStore.xml\"",
    #     f"adb push UPDATE-unSU-signed.zip /sdcard/",
    #     f"adb shell twrp install /sdcard/UPDATE-unSU-signed.zip",
    #     f"adb reboot"
    # ]

    # for cmd in commands:
    #     run_adb_command(cmd)

    # Bắt đầu reg gmail
    # Màn hình: 1. Chào mừng
    find_text_on_screen("Bắt đầu")
    time.sleep(random.randint(2, 8))
    random_input_swipe(x_range=(510, 563), y_range=(772, 837))

    # Màn hình: 2. Kết nối wifi
    find_text_on_screen("Tiếp theo")
    time.sleep(random.randint(2, 8))
    random_input_swipe(x_range=(752, 886), y_range=(1833, 1849))

    # Màn hình: 3. Đồng ý với các điều khoản điều kiện
    find_text_on_screen("Đồng ý với các Điều khoản và Điều kiện")
    time.sleep(random.randint(2, 8))
    for i in range(random.randint(2, 4)):
        random_input_swipe(x_range=(950, 1000), y_range=(1818, 1890)) # click để scroll xuống, random số lần click 2 - 4 lần
        time.sleep(random.randint(1, 3))

    random_input_swipe(x_range=(112, 134), y_range=(1630, 1654)) # click: tôi đã đọc và đồng ý với tất cả nội dung trên
    time.sleep(random.randint(2, 8))
    random_input_swipe(x_range=(748, 903), y_range=(1837, 1851)) # click: nút tiếp theo

    # Màn hình: 4. Tạo tài khoản gmail
    find_text_on_screen("Đăng nhập")
    time.sleep(random.randint(2, 8))
    random_input_swipe(x_range=(102, 309), y_range=(1453, 1463))

    # Màn hình: 5. Dành cho mục đích cá nhân của tôi
    find_text_on_screen("Dành cho mục đích cá nhân của tôi")
    time.sleep(random.randint(2, 8))
    random_input_swipe(x_range=(118, 677), y_range=(1018, 1024))

    # Màn hình: 6. Họ tên
    find_text_on_screen("Tạo Tài khoản Google")
    time.sleep(random.randint(2, 8))
    name = fake.name()
    parts = name.split()
    ## Họ
    first_name = parts[0]
    random_input_swipe(x_range=(126, 754), y_range=(682, 784))
    time.sleep(random.randint(2, 8))
    input_text_with_delay(first_name)
    time.sleep(random.randint(2, 8))
    ## Tên
    last_name = parts[-1]
    random_input_swipe(x_range=(118, 677), y_range=(1018, 1024))
    time.sleep(random.randint(2, 8))
    input_text_with_delay(last_name)
    time.sleep(random.randint(2, 8))
    ## Tiếp theo
    random_input_swipe(x_range=(118, 677), y_range=(1018, 1024))
    
    # Màn hình: 7. Tọa độ ngày tháng năm
    find_text_on_screen("Thông tin cơ bản")
    time.sleep(random.randint(2, 8))
    dob = fake.date_of_birth(minimum_age=18, maximum_age=45)
    day = dob.day
    month = dob.month
    year = dob.year

with open("config.txt", "r", encoding="utf-8") as f:
    lines = f.read().splitlines()
    wifiname = lines[0]
    wifipass = lines[1]
    url_proxy = lines[2]

try:
    # while True:
        if os.path.exists("ui.xml"):
            os.remove("ui.xml")

        # main(wifiname, wifipass, url_proxy)
        main("P1632", "Th@pnhjthj3u", url_proxy)

except KeyboardInterrupt:
    print("\nDa nhan lenh dung tu nguoi dung (Ctrl+C). Ket thuc chuong trinh.")
