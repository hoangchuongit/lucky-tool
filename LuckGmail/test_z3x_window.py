
import psycopg2
import os
import datetime

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

save_gmail_password("test@gmail.com", "123456")