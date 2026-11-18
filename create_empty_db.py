import sqlite3
import os
import sys

def create_empty_db(source_db, target_db):
    if not os.path.exists(source_db):
        print(f"[WARNING] Source database not found: {source_db}")
        return

    src_conn = sqlite3.connect(source_db)
    src_cursor = src_conn.cursor()

    src_cursor.execute("""
        SELECT type, name, sql FROM sqlite_master
        WHERE sql IS NOT NULL
        ORDER BY
            CASE type
                WHEN 'table' THEN 1
                WHEN 'index' THEN 2
                WHEN 'view'  THEN 3
                WHEN 'trigger' THEN 4
                ELSE 5
            END, name
    """)
    schema_rows = src_cursor.fetchall()
    src_conn.close()

    target_dir = os.path.dirname(target_db)
    if not os.path.exists(target_dir):
        os.makedirs(target_dir, exist_ok=True)

    if os.path.exists(target_db):
        os.remove(target_db)

    dst_conn = sqlite3.connect(target_db)
    for (obj_type, obj_name, sql) in schema_rows:
        try:
            dst_conn.execute(sql)
        except Exception as e:
            print(f"[WARNING] Skipped {obj_type} '{obj_name}': {e}")
    dst_conn.commit()
    dst_conn.close()

    size = os.path.getsize(target_db)
    print(f"[OK] Empty database created: {target_db} ({size} bytes)")
    print(f"[OK] Schema copied successfully from: {source_db}")

if __name__ == "__main__":
    base_dir = os.path.dirname(os.path.abspath(__file__))
    source = os.path.join(base_dir, "App_Bayegany", "Data", "bayegany.db")
    target = os.path.join(base_dir, "App_Bayegany", "Data", "bayegany_empty.db")
    
    # اگر مستقیماً داخل App_Bayegany نبود
    if not os.path.exists(source):
        alt_source = os.path.join(base_dir, "Data", "bayegany.db")
        if os.path.exists(alt_source):
            source = alt_source
            target = os.path.join(base_dir, "Data", "bayegany_empty.db")
            
    create_empty_db(source, target)
