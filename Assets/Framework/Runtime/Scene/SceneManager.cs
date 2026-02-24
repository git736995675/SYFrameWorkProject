using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

namespace SY
{
    public class SceneManager : MonoSingleton<SceneManager>
    {
        [Header("Settings")] [SerializeField] private float minLoadingTime = 1f; // 最小加载时间，确保加载画面显示足够时间
        [SerializeField] private float fadeDuration = 0.5f; // 淡入淡出时间

        [Header("References")] [SerializeField]
        private CanvasGroup loadingScreen; // 加载画面CanvasGroup

        private bool _isLoading = false;
        private string _currentActiveScene;

        protected override void Awake()
        {
            if (loadingScreen == null) return;
            loadingScreen.alpha = 0;
            loadingScreen.blocksRaycasts = false;
            loadingScreen.interactable = false;
        }

        public void SetLoadingScreen(CanvasGroup loadingScreen, float minLoadingTime = 1, float fadeDuration = 0.5f)
        {
            this.loadingScreen = loadingScreen;
            this.minLoadingTime = minLoadingTime;
            this.fadeDuration = fadeDuration;
        }

        // 加载场景（替换当前场景）
        public void LoadScene(string sceneName, Action<float> onProgress = null, Action onComplete = null)
        {
            if (_isLoading) return;

            ViewManager.Instance.CloseAllView();
            StartCoroutine(LoadSceneCoroutine(sceneName, LoadSceneMode.Single, onProgress, () =>
            {
                _currentActiveScene = sceneName;
                onComplete?.Invoke();
            }));
        }

        // 叠加加载场景（不卸载当前场景）
        public void LoadSceneAdditive(string sceneName, Action<float> onProgress = null, Action onComplete = null)
        {
            if (_isLoading) return;

            StartCoroutine(LoadSceneCoroutine(sceneName, LoadSceneMode.Additive, onProgress, () =>
            {
                _currentActiveScene = sceneName;
                onComplete?.Invoke();
            }));
        }

        // 卸载场景
        public void UnloadScene(string sceneName, Action onComplete = null)
        {
            if (_isLoading) return;

            StartCoroutine(UnloadSceneCoroutine(sceneName, () => { onComplete?.Invoke(); }));
        }

        // 重新加载当前场景
        public void ReloadCurrentScene(Action<float> onProgress = null, Action onComplete = null)
        {
            if (_isLoading) return;

            LoadScene(_currentActiveScene, onProgress, () => { onComplete?.Invoke(); });
        }

        // 获取当前活动场景名称
        public static string GetCurrentActiveScene()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        private IEnumerator LoadSceneCoroutine(string sceneName, LoadSceneMode mode, Action<float> onProgress,
            Action onComplete)
        {
            _isLoading = true;
            var startTime = Time.time;
            onProgress?.Invoke(0);

            // 显示加载画面
            yield return StartCoroutine(FadeLoadingScreen(0, 1, fadeDuration));

            // 异步加载场景
            var asyncOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, mode);
            if (asyncOp != null)
            {
                asyncOp.allowSceneActivation = false;
                float smoothProgress = 0;
                // 等待加载完成
                while (!asyncOp.isDone)
                {
                    // 平滑处理
                    smoothProgress = Mathf.Lerp(smoothProgress, 1, Time.deltaTime * 5f);

                    // 这里可以更新进度条 UI
                    onProgress?.Invoke(smoothProgress);

                    if (asyncOp.progress >= 0.9f && smoothProgress >= 0.99f)
                    {
                        onProgress?.Invoke(1);
                        // 确保最小加载时间
                        var elapsedTime = Time.time - startTime;
                        if (elapsedTime < minLoadingTime)
                        {
                            yield return new WaitForSeconds(minLoadingTime - elapsedTime);
                        }

                        asyncOp.allowSceneActivation = true;
                    }

                    yield return null;
                }
            }

            onComplete?.Invoke();

            // 隐藏加载画面
            yield return StartCoroutine(FadeLoadingScreen(1, 0, fadeDuration));

            _isLoading = false;

            if (loadingScreen)
                loadingScreen.gameObject.SetActive(false);
        }

        private IEnumerator UnloadSceneCoroutine(string sceneName, Action onComplete)
        {
            _isLoading = true;

            // 显示加载画面（如果需要）
            yield return StartCoroutine(FadeLoadingScreen(0, 1, fadeDuration));

            var asyncOp = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);

            while (asyncOp is { isDone: false })
            {
                yield return null;
            }

            onComplete?.Invoke();

            // 隐藏加载画面
            yield return StartCoroutine(FadeLoadingScreen(1, 0, fadeDuration));

            _isLoading = false;

            if (loadingScreen)
                loadingScreen.gameObject.SetActive(false);
        }

        private IEnumerator FadeLoadingScreen(float from, float to, float duration)
        {
            if (!loadingScreen) yield break;
            loadingScreen.gameObject.SetActive(true);
            loadingScreen.blocksRaycasts = to > 0;
            loadingScreen.interactable = to > 0;

            float elapsed = 0;
            while (elapsed < duration)
            {
                loadingScreen.alpha = Mathf.Lerp(from, to, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            loadingScreen.alpha = to;
        }
    }
}