import pygetwindow as gw
import pyautogui
import time
import os
import random

# Toàn cục cho vị trí cửa sổ
win_x, win_y = None, None

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

def get_window_coordinates_and_focus():
    global win_x, win_y
    print("Đang tìm cửa sổ Z3X Samsung Tool PRO v29.5 [G610F]...")
    try:
        windows = [win for win in gw.getAllWindows() if win.title.strip()]
        print("Các cửa sổ hợp lệ hiện có:", [win.title for win in windows])
        target_window = next(
            (win for win in windows if 'z3x samsung tool pro v29.5 [g610f]' in win.title.lower()),
            None
        )
        if not target_window:
            print("Không tìm thấy cửa sổ Z3X Samsung Tool PRO v29.5 [G610F].")
            return False
        print("Đã tìm thấy cửa sổ, đang focus...")
        if not target_window.isActive:
            target_window.activate()
            print("Đã focus vào cửa sổ.")
            time.sleep(1)
        if not target_window.isMaximized:
            print("Đang phóng to (maximize) cửa sổ...")
            target_window.maximize()
            time.sleep(1)
        return True
    except Exception as e:
        print(f"Đã xảy ra lỗi khi lấy tọa độ/focus cửa sổ: {e}")
        return False

def click_at_coordinates(x, y):
    print(f"Di chuyển chuột tới ({x},{y})")
    try:
        pyautogui.click(x, y, duration=0.5)
        print(f"Đã tới vị trí ({x}, {y}), click!")
        time.sleep(2)
    except Exception as e:
        print(f"Đã xảy ra lỗi khi click: {e}")

def main():
    while True:
        try:
            if get_window_coordinates_and_focus():
                print("Bước 1: Click vào tab Repair (350, 32)")
                click_at_coordinates(350, 32)

                print("Bước 2: Click vào dropdown để chọn thiết bị (350, 75)")
                click_at_coordinates(350, 75)

                print("Bước 3: Nhấn enter")
                pyautogui.press('pagedown')
                time.sleep(1)
                pyautogui.press('enter')

                print("Bước 4: Sinh số IMEI")
                random_numbers = generate_imei()

                print("Bước 5: Click vào (253, 128) và nhập IMEI")
                click_at_coordinates(253, 128)
                for num in random_numbers:
                    pyautogui.press(str(num))
                print(f"Đã nhập IMEI: {random_numbers}")

                print("Bước 6: Sinh số IMEI2")
                random_numbers1 = generate_imei()

                print("Bước 7: Click vào (253, 155) và nhập IMEI2")
                click_at_coordinates(253, 155)
                for num in random_numbers1:
                    pyautogui.press(str(num))
                print(f"Đã nhập IMEI2: {random_numbers1}")

                print("Bước 8: Click vào button Repair (1000, 130)")
                click_at_coordinates(1000, 130)
                time.sleep(60)

                print("Bước 9: Click loại bỏ tích ở IMEI và IMEI2 (253, 128) và (253, 155)")
                click_at_coordinates(253, 128)
                click_at_coordinates(253, 155)

                break
            else:
                print("Không thể lấy tọa độ cửa sổ, dừng chương trình.")
        except:
            pass

if __name__ == "__main__":
    main()
