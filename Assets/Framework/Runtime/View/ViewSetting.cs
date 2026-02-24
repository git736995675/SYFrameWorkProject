using UnityEngine;

namespace SY
{
    public class ViewSetting
    {
        // 是否是vr模式
        public static bool IsVr { get; set; } = false;

        // vr模式相机设置
        public static Camera Camera { get; set; }

        // 相机与ui的距离
        public static float CameraZoom { get; set; } = 80;

        // ui是否跟随相机
        public static bool IsFollow { get; set; } = false;
    }
}