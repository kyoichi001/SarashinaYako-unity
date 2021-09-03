using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kyoichi
{
    public class Context
    {
        public Context() { }
        public Context(Dictionary<string, object> dat)
        {
            data = dat;
        }
        public Dictionary<string, object> data = new Dictionary<string, object>();
    }

    public class SceneChangeWrapper : SingletonMonoBehaviour<SceneChangeWrapper>
    {

        public enum StackMode
        {
            AddToTop,
            SwapTop,
        }

        [SerializeField] GameObject loadingUI;
        [SerializeField] string fadeInAnimationName;
        [SerializeField] string fadeOutAnimationName;

        Stack<KeyValuePair<string, Context>> stack = new Stack<KeyValuePair<string, Context>>();

        GameObject uiInstance;
        Animator uiAnimator;


        private void Start()
        {
            uiInstance = Instantiate(loadingUI);
            uiAnimator = uiInstance.GetComponent<Animator>();
            DontDestroyOnLoad(this);
            DontDestroyOnLoad(uiInstance);
            uiInstance.SetActive(false);
            stack.Push(new KeyValuePair<string, Context>(SceneManager.GetActiveScene().name, null));
        }

        public async UniTask SceneChangeAsync(string targetScene, Context context, float fade = 1f, StackMode mode = StackMode.AddToTop)
        {
            // シーン切り替え
            uiInstance.SetActive(true);
            uiAnimator.Play(fadeInAnimationName);
            //アニメーションが再生しきったら
            await UniTask.WaitUntil(() => uiAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime == 1);
            if (mode == StackMode.AddToTop)
            {
                stack.Push(new KeyValuePair<string, Context>(targetScene, context));
            }
            else
            {
                stack.Pop();
                stack.Push(new KeyValuePair<string, Context>(targetScene, context));
            }
           await PlaySceneAsync(targetScene, context);
            uiAnimator.Play(fadeOutAnimationName);
            //アニメーションが再生しきったら
            await UniTask.WaitUntil(() => uiAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime == 1);
            uiInstance.SetActive(false);
        }

        public void Back(Context context = null, float fade = 1f)
        {
            stack.Pop();
            var top = stack.Peek();
            if (context == null)
            {
                PlaySceneAsync(top.Key, top.Value);
            }
            else
            {
                PlaySceneAsync(top.Key, context);
            }
        }

        public void Restart(Context context = null, float fade = 1f)
        {
            var top = stack.Peek();
            if (context == null)
            {
                PlaySceneAsync(top.Key, top.Value);
            }
            else
            {
                PlaySceneAsync(top.Key, context);
            }
        }

      async  UniTask PlaySceneAsync(string sceneName, Context context)
        {
            SceneManager.LoadScene(sceneName);
            await FindObjectOfType<SceneFunctioner>().SceneStartAsync(context);

        }

    }
}
