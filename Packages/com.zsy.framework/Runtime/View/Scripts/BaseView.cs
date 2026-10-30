using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SY
{
    // View基类（绑定UI元素）
    public abstract class BaseView : MonoBehaviour
    {
        public BaseModel Model { get; private set; }
        public string ViewName { get; set; }

        public Button[] buttons; //按钮集合
        public Toggle[] toggles; //toggle集合

        private ViewAnimation _animation; //动画

        #region 外部调用

        //外部调用初始化
        public void Initialize(BaseModel model)
        {
            Model = model;
            Model.AddChangeEvent(OnModelChanged);
            _animation = GetComponentInChildren<ViewAnimation>();
            RegisterEvents();
            Initialize();
            SetListener();
            CreateMask(true);
        }

        public void ShowView()
        {
            Show();
            Model.BaseArgs?.OpenEvent?.Invoke();
            PlayOpenAnimation();
        }

        public void HideView()
        {
            Model.BaseArgs?.CloseEvent?.Invoke();
            Model.RemoveChangeEvent(OnModelChanged);
            this.RemoveOwnerEvents();
            Hide();
            PlayHideAnimation();
        }

        #endregion


        #region 生命周期，继承用

        /// <summary>
        /// 初始化，加载结束第一时间调用
        /// </summary>
        protected virtual void Initialize()
        {
        }

        /// <summary>
        /// 展示时第一时间调用
        /// </summary>
        protected virtual void Show()
        {
        }

        /// <summary>
        /// 注册事件用，展示时调用
        /// </summary>
        protected virtual void RegisterEvents()
        {
        }

        /// <summary>
        /// 展示动画播放结束时调用
        /// </summary>
        protected virtual void ShowCompleted()
        {
        }

        /// <summary>
        /// Model数据变化时调用
        /// </summary>
        protected virtual void OnModelChanged()
        {
        }

        /// <summary>
        /// Update
        /// </summary>
        protected virtual void UIUpdate()
        {
        }

        /// <summary>
        /// 按钮的通用点击事件，所有按钮点击时都会调用
        /// </summary>
        protected virtual void CommonButtonClickEvent(Button button)
        {
            SyAudio.PlaySfx("ui.button");
        }

        /// <summary>
        /// Toggle的通用点击事件，所有按钮点击时都会调用
        /// </summary>
        protected virtual void CommonToggleClickEvent(Toggle toggle, bool isOn)
        {
            SyAudio.PlaySfx("ui.button");
        }

        /// <summary>
        /// 关闭时第一时间调用
        /// </summary>
        protected virtual void Hide()
        {
        }

        /// <summary>
        /// 关闭动画播放完毕调用
        /// </summary>
        protected virtual void HideCompleted()
        {
            Destroy(gameObject);
        }

        #endregion

        protected void CloseSelf()
        {
            ViewManager.Instance.CloseView(ViewName);
        }

        //设置通用监听
        private void SetListener()
        {
            buttons = GetComponentsInChildren<Button>();
            if (buttons != null)
            {
                foreach (var button in buttons)
                {
                    button.onClick.AddListener(() =>
                    {
                        CommonButtonClickEvent(button);
                    });
                }
            }

            toggles = GetComponentsInChildren<Toggle>();
            if (toggles == null) return;
            foreach (var toggle in toggles)
            {
                toggle.onValueChanged.AddListener((isOn) =>
                {
                    CommonToggleClickEvent(toggle, isOn);
                });
            }
        }

        //生成展示和关闭时的遮罩
        private void CreateMask(bool showMask)
        {
            if (Model.layer is UILayer.Background or UILayer.Normal) return;
            var mask = new GameObject().AddComponent<Image>();
            if (showMask)
            {
                mask.raycastTarget = Model.isMaskClose;
            }

            mask.name = showMask ? "ShowMask" : "HideMask";
            mask.transform.SetParent(transform);
            mask.transform.localPosition = Vector3.zero;
            mask.transform.localScale = Vector3.one;
            mask.color = new Color(0, 0, 0, showMask ? Model.maskAlpha : 0);
            var rectTransform = mask.GetComponent<RectTransform>();

            // 设置锚点为四角拉伸
            rectTransform.anchorMin = Vector2.zero; // (0,0)
            rectTransform.anchorMax = Vector2.one; // (1,1)
            // 设置边距为0
            rectTransform.offsetMin = Vector2.zero; // Left和Bottom
            rectTransform.offsetMax = Vector2.zero; // Right和Top

            if (showMask)
            {
                mask.transform.SetAsFirstSibling();

                if (!Model.isMaskClose) return;
                mask.AddComponent<Button>().onClick.AddListener(() =>
                {
                    var btn = mask.GetComponent<Button>();
                    CommonButtonClickEvent(btn);
                    btn.enabled = false;
                    CloseSelf();
                });
            }
            else
            {
                mask.transform.SetAsLastSibling();
            }
        }

        private void Update()
        {
            UIUpdate();
        }

        /// 打开动画播放
        private void PlayOpenAnimation()
        {
            if (_animation)
            {
                _animation.PlayOpen(_animation.transform, ShowCompleted);
            }
            else
            {
                gameObject.SetActive(true);
                ShowCompleted();
            }
        }

        /// <summary>
        /// 关闭动画播放
        /// </summary>
        private void PlayHideAnimation()
        {
            if (_animation)
            {
                CreateMask(false);
                _animation.PlayHide(_animation.transform, () =>
                {
                    ViewManager.Instance.CheckWaitingView();
                    HideCompleted();
                });
            }
            else
            {
                gameObject.SetActive(false);
                HideCompleted();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("为UI重新赋值")]
        private void DoCustomAction()
        {
            buttons = transform.GetComponentsInChildren<Button>();
            toggles = transform.GetComponentsInChildren<Toggle>();
            AutoAssignTool.ProcessSelectedPrefab(gameObject);
            EditorUtility.SetDirty(gameObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif
    }
}