import bpy
import re

folder = "Level3"
txt_file="C:\\Users\\Julius\\Documents\\Projects\\toneDEATH\\Assets\\Models\\LevelTemplates\\Level3\\template_data.txt"

def parseQuaternion(str):
    return re.sub("<Quaternion \(w=|, x=|, y=|, z=|\)>", "||", str).rstrip("||")

f = open(txt_file, 'w')
for item in bpy.context.selected_objects:
    item.rotation_mode = "QUATERNION"
    f.writelines(str(item.name).split('.')[0] + '||'
    + str(item.location[0]) + '||'
    + str(item.location[1]) + '||'
    + str(item.location[2])
    + parseQuaternion(str(item.rotation_quaternion)) + "\n")
    item.rotation_mode = "XYZ"
f.close()