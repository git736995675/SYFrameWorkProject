using ArabicTool;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SY
{
    public class LocalizationText : MonoBehaviour
    {
        [SerializeField] private int localizationKey; // 文本的key
        [SerializeField] private bool isFontOpenLocalize = true; // 字体是否开启本地化

        private Text _textComponent;
        private TMP_Text _tmpTextComponent;
        private object[] _param;
        private bool _useTextMeshPro = false;

        private void Awake()
        {
            if (GetComponent<TMP_Text>() != null)
            {
                _useTextMeshPro = true;
                _tmpTextComponent = GetComponent<TMP_Text>();
            }
            else
            {
                _useTextMeshPro = false;
                _textComponent = GetComponent<Text>();
            }

            UpdateText();

            if (Application.isPlaying)
            {
                LanguageManager.OnLocalizeChangedListener += UpdateText;
            }
        }

        protected void OnDestroy()
        {
            if (Application.isPlaying)
            {
                LanguageManager.OnLocalizeChangedListener -= UpdateText;
            }
        }

        /// <summary>
        /// 重新本地化，用于游戏内切换语言时调用
        /// </summary>
        private void UpdateText()
        {
            if (isFontOpenLocalize)
            {
                if (_useTextMeshPro)
                {
                    if (LanguageManager.Instance.CurrentCountry.TMPFontAsset)
                        _tmpTextComponent.font = LanguageManager.Instance.CurrentCountry.TMPFontAsset;
                }
                else
                {
                    if (LanguageManager.Instance.CurrentCountry.Font)
                        _textComponent.font = LanguageManager.Instance.CurrentCountry.Font;
                }
            }

            LanguageManager.GetStr(localizationKey, text =>
            {
                if (ArabicTextTool.HasArabicChar(text))
                {
                    DoSetArabText(text);
                }
                else
                {
                    SetText(text);
                }
            }, _param);
        }

        private void DoSetArabText(string orgStr, float width = 0.0f, float height = 0.0f)
        {
            SetText(orgStr);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 编辑器模式修改
        /// </summary>
        public void EditorOnLocalize(CountryAndCode language)
        {
            if (isFontOpenLocalize)
            {
                if (GetComponent<TMP_Text>() != null)
                {
                    _useTextMeshPro = true;
                    _tmpTextComponent = GetComponent<TMP_Text>();

                    if (language.TMPFontAsset)
                        _tmpTextComponent.font = language.TMPFontAsset;
                }

                if (GetComponent<Text>() != null)
                {
                    _useTextMeshPro = false;
                    _textComponent = GetComponent<Text>();

                    if (language.Font)
                        _textComponent.font = language.Font;
                }
            }

            LanguageManager.GetStr(localizationKey, language, text =>
            {
                if (ArabicTextTool.HasArabicChar(text))
                {
                    DoSetArabText(text);
                }
                else
                {
                    SetText(text);
                }
            }, _param);
        }

        public void SetText(int key, params object[] param)
        {
            localizationKey = key;
            _param = param;
            UpdateText();
        }

        public void SetText(params object[] param)
        {
            _param = param;
            UpdateText();
        }

        public void SetText(string text)
        {
            if (_useTextMeshPro)
            {
                if (_textComponent)
                    _tmpTextComponent.text = text;
            }
            else
            {
                if (_textComponent)
                    _textComponent.text = text;
            }
        }
    }
}