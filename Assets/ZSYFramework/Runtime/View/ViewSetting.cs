using UnityEngine;

namespace ZSYFramework.Runtime.View
{
    public class ViewSetting
    {
        // �Ƿ���vrģʽ
        public static bool IsVr { get; set; } = false;

        // vrģʽ�������
        public static Camera Camera { get; set; }

        // �����ui�ľ���
        public static float CameraZoom { get; set; } = 80;

        // ui�Ƿ�������
        public static bool IsFollow { get; set; } = false;
    }
}