using System;
using System.Linq;
using UnityEngine;

namespace SY
{
    /// <summary>
    /// 扩展Component
    /// </summary>
    public static class ComponentExt
    {
        /// <summary>
        ///  根据名称查找物体 默认返回查找到的第一个
        /// </summary>
        public static T Search<T>(this Component transform, string name) where T : Component
        {
            var trs = transform.GetComponentsInChildren<T>(true);
            return trs.FirstOrDefault(t => t.name == name);
        }

        /// <summary>
        ///     根据名称查找物体 默认返回查找到的第一个
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="transform">父物体</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static T Search<T>(this GameObject transform, string name) where T : Component
        {
            var trs = transform.GetComponentsInChildren<T>(true);
            return trs.FirstOrDefault(t => t.name == name);
        }

        /// <summary>
        ///    根据名称查找物体 默认返回查找到的第一个
        /// </summary>
        /// <param name="transform">父物体</param>
        /// <param name="name">名称</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static Component Search(this Component transform, string name, Type type)
        {
            var trs = transform.GetComponentsInChildren(type, true);
            return trs.FirstOrDefault(t => t.name == name);
        }

        /// <summary>
        /// 播放组件下面所有粒子特效
        /// </summary>
        /// <param name="transform"></param>
        public static void PlayAllParticleSystem(this Component transform)
        {
            var ps = transform.GetComponentsInChildren<ParticleSystem>();
            if (ps.Length <= 0) return;
            foreach (var item in ps)
            {
                item.SetActive(true);
                item.Play();
            }
        }

        /// <summary>
        /// 播放组件下面所有粒子特效
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="color"></param>
        public static void PlayAllParticleSystem(this Component transform, Color color)
        {
            var ps = transform.GetComponentsInChildren<ParticleSystem>();
            if (ps.Length <= 0) return;
            foreach (var item in ps)
            {
                var main = item.main;
                main.startColor = color;
                item.SetActive(true);
                item.Play();
            }
        }

        /// <summary>
        /// 播放组件下面所有粒子特效
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="layout"></param>
        public static void PlayAllParticleSystem(this Component transform, int layout)
        {
            var ps = transform.GetComponentsInChildren<ParticleSystem>();
            if (ps.Length <= 0) return;
            foreach (var item in ps)
            {
                item.GetComponent<Renderer>().sortingOrder = layout;
                item.SetActive(true);
                item.Play();
            }
        }

        public static T SetActive<T>(this T transform, bool isActive) where T : Component
        {
            transform.gameObject.SetActive(isActive);
            return transform;
        }

        /// <summary>
        ///     物体获取组件 为null则自动添加
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="transform">父物体</param>
        /// <returns></returns>
        public static T GetOrAddComponent<T>(this Component transform) where T : Component
        {
            var t = transform.GetComponent<T>();
            if (t == null)
            {
                t = transform.gameObject.AddComponent<T>();
            }

            return t;
        }

        /// <summary>
        /// 设置父物体
        /// </summary>
        /// <param name="gameObject">父物体</param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static GameObject SetParent(this GameObject gameObject, Transform parent)
        {
            gameObject.transform.SetParent(parent);
            return gameObject;
        }

        /// <summary>
        /// 设置父物体
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="gameObject">父物体</param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static T SetParent<T>(this T gameObject, Transform parent) where T : Component
        {
            gameObject.transform.SetParent(parent);
            return gameObject;
        }

        /// <summary>
        ///     物体获取组件 为null则自动添加
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="gameObject">父物体</param>
        /// <returns></returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.transform.GetOrAddComponent<T>();
        }


        /// <summary>
        ///     位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="position"></param>
        /// <returns>Position</returns>
        public static T Position<T>(this T selfComponent, Vector3 position) where T : Component
        {
            selfComponent.transform.position = position;
            return selfComponent;
        }

        /// <summary>
        ///     位置信息
        /// </summary>
        /// <param name="selfComponent">该物体</param>
        /// <param name="position"></param>
        /// <returns>Position</returns>
        public static GameObject Position(this GameObject selfComponent, Vector3 position)
        {
            selfComponent.transform.position = position;
            return selfComponent;
        }

        /// <summary>
        ///     世界位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns> new Vector3(x, y, z)</returns>
        public static T Position<T>(this T selfComponent, float x, float y, float z = 0) where T : Component
        {
            selfComponent.transform.position = new Vector3(x, y, z);
            return selfComponent;
        }

        /// <summary>
        ///     世界位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <returns> 类型 </returns>
        public static T PositionX<T>(this T selfComponent, float x) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.position;
            transform.position = new Vector3(x, tempPos.y, tempPos.z);
            return selfComponent;
        }

        /// <summary>
        ///     世界位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="y"></param>
        /// <returns> new Vector3(x, y, z)</returns>
        public static T PositionY<T>(this T selfComponent, float y) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.position;
            transform.position = new Vector3(tempPos.x, y, tempPos.z);
            return selfComponent;
        }

        /// <summary>
        ///     世界位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns> new Vector3(x, y, z)</returns>
        public static T PositionXY<T>(this T selfComponent, float x, float y) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.position;
            transform.position = new Vector3(x, y, tempPos.z);
            return selfComponent;
        }

        /// <summary>
        ///     世界位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="z"></param>
        /// <returns> new Vector3(x, y, z)</returns>
        public static T PositionZ<T>(this T selfComponent, float z) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.position;
            transform.position = new Vector3(tempPos.x, tempPos.y, z);
            return selfComponent;
        }

        /// <summary>
        ///     世界位置信息
        /// </summary>
        public static GameObject Position(this GameObject selfComponent, float x, float y, float z = 0)
        {
            selfComponent.transform.position = new Vector3(x, y, z);
            return selfComponent;
        }

        /// <summary>
        ///     世界位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="xyz"></param>
        /// <returns> Vector3.one * xyz </returns>
        public static T Position<T>(this T selfComponent, float xyz) where T : Component
        {
            selfComponent.transform.position = Vector3.one * xyz;
            return selfComponent;
        }

        /// <summary>
        ///     世界位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <returns> selfComponent.transform.Position </returns>
        public static Vector3 Position<T>(this T selfComponent) where T : Component
        {
            return selfComponent.transform.position;
        }

        /// <summary>
        ///     世界位置信息
        /// </summary>
        public static Vector3 Position(this GameObject selfComponent)
        {
            return selfComponent.transform.position;
        }

        /// <summary>
        ///     世界角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="rotation"></param>
        /// <returns> Rotation </returns>
        public static T Rotation<T>(this T selfComponent, Quaternion rotation) where T : Component
        {
            selfComponent.transform.rotation = rotation;
            return selfComponent;
        }

        /// <summary>
        ///     世界角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <returns> selfComponent.transform.Rotation </returns>
        public static Quaternion Rotation<T>(this T selfComponent) where T : Component
        {
            return selfComponent.transform.rotation;
        }

        /// <summary>
        ///     世界角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns> new Vector3(x, y, z) </returns>
        public static T EulerAngles<T>(this T selfComponent, float x, float y, float z) where T : Component
        {
            selfComponent.transform.eulerAngles = new Vector3(x, y, z);
            return selfComponent;
        }

        /// <summary>
        ///     世界角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <returns> new Vector3(x, y, z) </returns>
        public static T EulerAnglesX<T>(this T selfComponent, float x) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.eulerAngles;
            transform.eulerAngles = new Vector3(x, tempPos.y, tempPos.z);
            return selfComponent;
        }

        /// <summary>
        ///     世界角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="y"></param>
        /// <returns> new Vector3(x, y, z) </returns>
        public static T EulerAnglesY<T>(this T selfComponent, float y) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.eulerAngles;
            transform.eulerAngles = new Vector3(tempPos.x, y, tempPos.z);
            return selfComponent;
        }


        /// <summary>
        ///     世界角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns> new Vector3(x, y, z) </returns>
        public static T EulerAnglesXY<T>(this T selfComponent, float x, float y) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.eulerAngles;
            transform.eulerAngles = new Vector3(x, y, tempPos.z);
            return selfComponent;
        }


        /// <summary>
        ///     世界角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="z"></param>
        /// <returns> new Vector3(x, y, z) </returns>
        public static T EulerAnglesZ<T>(this T selfComponent, float z) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.eulerAngles;
            transform.eulerAngles = new Vector3(tempPos.x, tempPos.y, z);
            return selfComponent;
        }

        /// <summary>
        ///     世界角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="eulerAngles"></param>
        /// <returns> EulerAngles </returns>
        public static T EulerAngles<T>(this T selfComponent, Vector3 eulerAngles) where T : Component
        {
            selfComponent.transform.eulerAngles = eulerAngles;
            return selfComponent;
        }

        /// <summary>
        ///     世界角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <returns> selfComponent.transform.localEulerAngles </returns>
        public static Vector3 EulerAngles<T>(this T selfComponent) where T : Component
        {
            return selfComponent.transform.eulerAngles;
        }

        /// <summary>
        ///     自身缩放信息
        /// </summary>
        /// <param name="selfComponent">该物体</param>
        /// <returns> selfComponent.transform.LocalScale </returns>
        public static Vector3 LocalScale(this GameObject selfComponent)
        {
            return selfComponent.transform.localScale;
        }

        /// <summary>
        ///     自身缩放信息
        /// </summary>
        /// <param name="selfComponent">该物体</param>
        /// <returns> selfComponent.transform.LocalScale </returns>
        public static Vector3 LocalScale<T>(this T selfComponent) where T : Component
        {
            return selfComponent.transform.localScale;
        }

        /// <summary>
        ///     自身缩放信息
        /// </summary>
        public static GameObject LocalScale(this GameObject selfComponent, float xyz)
        {
            selfComponent.transform.localScale = Vector3.one * xyz;
            return selfComponent;
        }

        /// <summary>
        ///     自身缩放信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="xyz"></param>
        /// <returns> Vector3.one * xyz </returns>
        public static T LocalScale<T>(this T selfComponent, float xyz) where T : Component
        {
            selfComponent.transform.localScale = Vector3.one * xyz;
            return selfComponent;
        }

        /// <summary>
        ///     自身缩放信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns> new Vector3(x, y, z) </returns>
        public static T LocalScale<T>(this T selfComponent, float x, float y, float z) where T : Component
        {
            selfComponent.transform.localScale = new Vector3(x, y, z);
            return selfComponent;
        }

        /// <summary>
        ///     自身缩放信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <returns> new Vector3(x, y, z) </returns>
        public static T LocalScaleX<T>(this T selfComponent, float x) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.localScale;
            transform.localScale = new Vector3(x, tempPos.y, tempPos.z);
            return selfComponent;
        }

        /// <summary>
        ///     自身缩放信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="y"></param>
        /// <returns> new Vector3(x, y, z) </returns>
        public static T LocalScaleY<T>(this T selfComponent, float y) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.localScale;
            transform.localScale = new Vector3(tempPos.x, y, tempPos.z);
            return selfComponent;
        }

        /// <summary>
        ///     自身缩放信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="z"></param>
        /// <returns> new Vector3(x, y, z) </returns>
        public static T LocalScaleZ<T>(this T selfComponent, float z) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.localScale;
            transform.localScale = new Vector3(tempPos.x, tempPos.y, z);
            return selfComponent;
        }

        /// <summary>
        ///     自身缩放信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns> new Vector3(x, y, z) </returns>
        public static T LocalScaleXY<T>(this T selfComponent, float x, float y) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.localScale;
            transform.localScale = new Vector3(x, y, tempPos.z);
            return selfComponent;
        }

        /// <summary>
        ///     自身缩放信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="xy"></param>
        /// <returns> new Vector3(x, y, z) </returns>
        public static T LocalScaleXY<T>(this T selfComponent, float xy) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.localScale;
            transform.localScale = new Vector3(tempPos.x * xy, tempPos.y * xy, tempPos.z);
            return selfComponent;
        }

        /// <summary>
        ///     自身缩放信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="scale"></param>
        /// <returns> scale </returns>
        public static T LocalScale<T>(this T selfComponent, Vector3 scale) where T : Component
        {
            selfComponent.transform.localScale = scale;
            return selfComponent;
        }

        /// <summary>
        ///     自身位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="position"></param>
        /// <returns> Position </returns>
        public static T LocalPosition<T>(this T selfComponent, Vector3 position) where T : Component
        {
            selfComponent.transform.localPosition = position;
            return selfComponent;
        }

        /// <summary>
        ///     自身位置信息
        /// </summary>
        /// <param name="selfComponent">该物体</param>
        /// <param name="position"></param>
        /// <returns> Position </returns>
        public static GameObject LocalPosition(this GameObject selfComponent, Vector3 position)
        {
            selfComponent.transform.localPosition = position;
            return selfComponent;
        }

        /// <summary>
        ///     自身位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="xyz"></param>
        /// <returns> Vector3.one *xyz </returns>
        public static T LocalPosition<T>(this T selfComponent, float xyz) where T : Component
        {
            selfComponent.transform.localPosition = Vector3.one * xyz;
            return selfComponent;
        }

        /// <summary>
        ///     自身位置信息
        /// </summary>
        public static GameObject LocalPosition(this GameObject selfComponent, float xyz)
        {
            selfComponent.transform.localPosition = Vector3.one * xyz;
            return selfComponent;
        }

        /// <summary>
        ///     自身位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns> new Vector3(x, y, z) </returns>
        public static T LocalPosition<T>(this T selfComponent, float x, float y, float z) where T : Component
        {
            selfComponent.transform.localPosition = new Vector3(x, y, z);
            return selfComponent;
        }

        /// <summary>
        ///     自身位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <returns> 类型 </returns>
        public static T LocalPositionX<T>(this T selfComponent, float x) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.localPosition;
            transform.localPosition = new Vector3(x, tempPos.y, tempPos.z);
            return selfComponent;
        }

        /// <summary>
        ///     自身位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="y"></param>
        /// <returns> 类型 </returns>
        public static T LocalPositionY<T>(this T selfComponent, float y) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.localPosition;
            transform.localPosition = new Vector3(tempPos.x, y, tempPos.z);
            return selfComponent;
        }

        /// <summary>
        ///     自身位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="z"></param>
        /// <returns> 类型 </returns>
        public static T LocalPositionZ<T>(this T selfComponent, float z) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.localPosition;
            transform.localPosition = new Vector3(tempPos.x, tempPos.y, z);
            return selfComponent;
        }

        /// <summary>
        ///     自身位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns> 类型 </returns>
        public static T LocalPositionXY<T>(this T selfComponent, float x, float y) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.localPosition;
            transform.localPosition = new Vector3(x, y, tempPos.z);
            return selfComponent;
        }

        /// <summary>
        ///     自身位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <returns> selfComponent.transform.LocalPosition </returns>
        public static Vector3 LocalPosition<T>(this T selfComponent) where T : Component
        {
            return selfComponent.transform.localPosition;
        }

        /// <summary>
        ///     自身位置信息
        /// </summary>
        /// <param name="selfComponent">该物体</param>
        /// <returns> selfComponent.transform.LocalPosition </returns>
        public static Vector3 LocalPosition(this GameObject selfComponent)
        {
            return selfComponent.transform.localPosition;
        }

        /// <summary>
        ///     自身角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="rotation"></param>
        /// <returns> Rotation </returns>
        public static T LocalRotation<T>(this T selfComponent, Quaternion rotation) where T : Component
        {
            selfComponent.transform.localRotation = rotation;
            return selfComponent;
        }

        /// <summary>
        ///     自身角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <returns> selfComponent.transform.localRotation </returns>
        public static Quaternion LocalRotation<T>(this T selfComponent) where T : Component
        {
            return selfComponent.transform.localRotation;
        }

        /// <summary>
        ///     自身角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="eulerAngles"></param>
        /// <returns> EulerAngles </returns>
        public static T LocalEulerAngles<T>(this T selfComponent, Vector3 eulerAngles) where T : Component
        {
            selfComponent.transform.localEulerAngles = eulerAngles;
            return selfComponent;
        }

        /// <summary>
        ///     自身角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns> new Vector3(x, y, z) </returns>
        public static T LocalEulerAngles<T>(this T selfComponent, float x, float y, float z) where T : Component
        {
            selfComponent.transform.localEulerAngles = new Vector3(x, y, z);
            return selfComponent;
        }

        /// <summary>
        ///     自身角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <returns> 类型 </returns>
        public static T LocalEulerAnglesX<T>(this T selfComponent, float x) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.localEulerAngles;
            transform.localEulerAngles = new Vector3(x, tempPos.y, tempPos.z);
            return selfComponent;
        }

        /// <summary>
        ///     自身角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="y"></param>
        /// <returns> new Vector3(x, y, z) </returns>
        public static T LocalEulerAnglesY<T>(this T selfComponent, float y) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.localEulerAngles;
            transform.localEulerAngles = new Vector3(tempPos.x, y, tempPos.z);
            return selfComponent;
        }

        /// <summary>
        ///     自身角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="z"></param>
        /// <returns> new Vector3(x, y, z) </returns>
        public static T LocalEulerAnglesZ<T>(this T selfComponent, float z) where T : Component
        {
            var transform = selfComponent.transform;
            var tempPos = transform.localEulerAngles;
            transform.localEulerAngles = new Vector3(tempPos.x, tempPos.y, z);
            return selfComponent;
        }

        /// <summary>
        ///     自身角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="xyz"></param>
        /// <returns>  Vector3.one *xyz </returns>
        public static T LocalEulerAngles<T>(this T selfComponent, float xyz) where T : Component
        {
            selfComponent.transform.localEulerAngles = Vector3.one * xyz;
            return selfComponent;
        }

        /// <summary>
        ///     自身角度信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <returns> selfComponent.transform.localEulerAngles </returns>
        public static Vector3 LocalEulerAngles<T>(this T selfComponent) where T : Component
        {
            return selfComponent.transform.localEulerAngles;
        }

        /// <summary>
        ///     UI坐标信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="position"></param>
        /// <returns> Position </returns>
        public static T AnchoredPosition3D<T>(this T selfComponent, Vector3 position) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().anchoredPosition3D = position;
            return selfComponent;
        }

        /// <summary>
        ///     UI坐标信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns> new Vector3(x, y, z) </returns>
        public static T AnchoredPosition3D<T>(this T selfComponent, float x, float y, float z) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(x, y, z);
            return selfComponent;
        }

        /// <summary>
        ///     UI坐标信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <returns> anchoredPosition3D </returns>
        public static Vector3 AnchoredPosition3D<T>(this T selfComponent) where T : Component
        {
            return selfComponent.GetComponent<RectTransform>().anchoredPosition3D;
        }

        /// <summary>
        ///     UI坐标信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="position"></param>
        /// <returns> Position </returns>
        public static T AnchoredPosition<T>(this T selfComponent, Vector2 position) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().anchoredPosition = position;
            return selfComponent;
        }

        /// <summary>
        ///     UI坐标信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns> new Vector2(x, y) </returns>
        public static T AnchoredPosition<T>(this T selfComponent, float x, float y) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
            return selfComponent;
        }

        /// <summary>
        ///     UI坐标信息
        /// </summary>
        public static T AnchoredPositionX<T>(this T selfComponent, float x) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(x, selfComponent.GetComponent<RectTransform>().anchoredPosition.y);
            return selfComponent;
        }

        /// <summary>
        ///     UI坐标信息
        /// </summary>
        public static T AnchoredPosition<T>(this T selfComponent, float y) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(selfComponent.GetComponent<RectTransform>().anchoredPosition.x, y);
            return selfComponent;
        }

        /// <summary>
        ///     UI坐标信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <returns> anchoredPosition </returns>
        public static Vector2 AnchoredPosition<T>(this T selfComponent) where T : Component
        {
            return selfComponent.GetComponent<RectTransform>().anchoredPosition;
        }

        /// <summary>
        ///     UI尺寸大小
        /// </summary>
        public static T SizeDelta<T>(this T selfComponent, Vector2 sizeDelta) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().sizeDelta = sizeDelta;
            return selfComponent;
        }

        /// <summary>
        ///     UI尺寸大小
        /// </summary>
        public static T SizeDelta<T>(this T selfComponent, float xy) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().sizeDelta *= xy;
            return selfComponent;
        }

        /// <summary>
        ///     UI尺寸大小
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns> new Vector2(x, y) </returns>
        public static T SizeDelta<T>(this T selfComponent, float x, float y) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().sizeDelta = new Vector2(x, y);
            return selfComponent;
        }

        /// <summary>
        ///     UI尺寸大小
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <returns> sizeDelta </returns>
        public static Vector2 SizeDelta<T>(this T selfComponent) where T : Component
        {
            return selfComponent.GetComponent<RectTransform>().sizeDelta;
        }

        /// <summary>
        ///     锚点在四角时设置
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns> new Vector2(x, y) </returns>
        public static T OffsetMax<T>(this T selfComponent, float x, float y) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().offsetMax = new Vector2(x, y);
            return selfComponent;
        }

        /// <summary>
        ///     UI右上角的位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="xy"></param>
        /// <returns> OffsetMax *= xy </returns>
        public static T OffsetMax<T>(this T selfComponent, float xy) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().offsetMax *= xy;
            return selfComponent;
        }

        /// <summary>
        ///     UI右上角的位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        public static T OffsetMax<T>(this T selfComponent, Vector2 offsetMax) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().offsetMax = offsetMax;
            return selfComponent;
        }

        /// <summary>
        ///     UI右上角的位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <returns> OffsetMax </returns>
        public static Vector2 OffsetMax<T>(this T selfComponent) where T : Component
        {
            return selfComponent.GetComponent<RectTransform>().offsetMax;
        }

        /// <summary>
        ///     UI左下角的位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns> new Vector2(x, y) </returns>
        public static T OffsetMin<T>(this T selfComponent, float x, float y) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().offsetMin = new Vector2(x, y);
            return selfComponent;
        }

        /// <summary>
        ///     UI左下角的位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="xy"></param>
        /// <returns> offsetMin *= xy </returns>
        public static T OffsetMin<T>(this T selfComponent, float xy) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().offsetMin *= xy;
            return selfComponent;
        }

        /// <summary>
        ///     UI左下角的位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <param name="offsetMin"></param>
        /// <returns> offsetMin </returns>
        public static T OffsetMin<T>(this T selfComponent, Vector2 offsetMin) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().offsetMin = offsetMin;
            return selfComponent;
        }

        /// <summary>
        ///     UI左下角的位置信息
        /// </summary>
        /// <param name="selfComponent">该物体</param>
        /// <param name="offsetMin"></param>
        /// <returns> offsetMin </returns>
        public static GameObject OffsetMin(this GameObject selfComponent, Vector2 offsetMin)
        {
            selfComponent.GetComponent<RectTransform>().offsetMin = offsetMin;
            return selfComponent;
        }

        /// <summary>
        ///     UI左下角的位置信息
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="selfComponent">该物体</param>
        /// <returns> offsetMin </returns>
        public static Vector2 OffsetMin<T>(this T selfComponent) where T : Component
        {
            return selfComponent.GetComponent<RectTransform>().offsetMin;
        }

        /// <summary>
        ///     UI左下角的位置信息
        /// </summary>
        /// <param name="selfComponent">该物体</param>
        /// <returns> offsetMin </returns>
        public static Vector2 OffsetMin(this GameObject selfComponent)
        {
            return selfComponent.GetComponent<RectTransform>().offsetMin;
        }

        /// <summary>
        /// UI 锚点min信息
        /// </summary>
        /// <param name="selfComponent">该物体</param>
        /// <param name="anchorMin">要修改的值</param>
        /// <typeparam name="T">物体类型</typeparam>
        /// <returns> anchorMin </returns>
        public static T AnchorMin<T>(this T selfComponent, Vector2 anchorMin) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().anchorMin = anchorMin;
            return selfComponent;
        }

        /// <summary>
        /// UI 锚点min信息
        /// </summary>
        /// <param name="selfComponent">该物体</param>
        /// <param name="bate">倍数</param>
        /// <typeparam name="T">物体类型</typeparam>
        /// <returns> anchorMin </returns>
        public static T AnchorMin<T>(this T selfComponent, float bate) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().anchorMin = Vector2.one * bate;
            return selfComponent;
        }

        /// <summary>
        /// UI 锚点min信息
        /// </summary>
        /// <param name="selfComponent">该物体</param>
        /// <param name="anchorMin">要修改的值</param>
        /// <returns> anchorMin </returns>
        public static GameObject AnchorMin(this GameObject selfComponent, Vector2 anchorMin)
        {
            selfComponent.GetComponent<RectTransform>().anchorMin = anchorMin;
            return selfComponent;
        }

        /// <summary>
        /// UI 锚点min信息
        /// </summary>
        /// <param name="selfComponent">该物体</param>
        /// <param name="bate">要修改的值</param>
        /// <returns> anchorMin </returns>
        public static GameObject AnchorMin(this GameObject selfComponent, float bate)
        {
            selfComponent.GetComponent<RectTransform>().anchorMin = Vector2.one * bate;
            return selfComponent;
        }

        /// <summary>
        /// UI 锚点max信息
        /// </summary>
        /// <param name="selfComponent">该物体</param>
        /// <param name="anchorMax">要修改的值</param>
        /// <typeparam name="T">物体类型</typeparam>
        /// <returns> anchorMax </returns>
        public static T AnchorMax<T>(this T selfComponent, Vector2 anchorMax) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().anchorMax = anchorMax;
            return selfComponent;
        }

        /// <summary>
        /// UI 锚点max信息
        /// </summary>
        /// <param name="selfComponent">该物体</param>
        /// <param name="bate">要修改的值</param>
        /// <typeparam name="T">物体类型</typeparam>
        /// <returns> anchorMax </returns>
        public static T AnchorMax<T>(this T selfComponent, float bate) where T : Component
        {
            selfComponent.GetComponent<RectTransform>().anchorMax = Vector2.one * bate;
            return selfComponent;
        }

        /// <summary>
        /// UI 锚点max信息
        /// </summary>
        /// <param name="selfComponent">该物体</param>
        /// <param name="anchorMax">要修改的值</param>
        /// <returns> anchorMax </returns>
        public static GameObject AnchorMax(this GameObject selfComponent, Vector2 anchorMax)
        {
            selfComponent.GetComponent<RectTransform>().anchorMax = anchorMax;
            return selfComponent;
        }

        /// <summary>
        /// UI 锚点max信息
        /// </summary>
        public static GameObject AnchorMax(this GameObject selfComponent, float bate)
        {
            selfComponent.GetComponent<RectTransform>().anchorMax = Vector2.one * bate;
            return selfComponent;
        }

        /// <summary>
        ///  获取物体的RectTransform
        /// </summary>
        public static RectTransform GetRect<T>(this T selfComponent) where T : Component
        {
            return selfComponent.GetComponent<RectTransform>();
        }

        /// <summary>
        ///  获取物体的RectTransform
        /// </summary>
        public static RectTransform GetRect(this GameObject selfComponent)
        {
            return selfComponent.GetComponent<RectTransform>();
        }

        public static T NameReplace<T>(this T selfComponent) where T : UnityEngine.Object
        {
            selfComponent.name = selfComponent.name.Replace("(Clone)", "");
            return selfComponent;
        }
    }
}