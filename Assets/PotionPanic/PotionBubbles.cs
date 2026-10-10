using UnityEngine;
using UnityEngine.UI;

namespace PotionPanic
{
    // The old artwork had bubbles rising out of the potion. Animate UI Images to keep that effect.
    public sealed class PotionBubbles : MonoBehaviour
    {
        readonly Image[] bubbles = new Image[8];
        Color tint;
        float elapsed;
        bool moving;
        public void Initialize(Color color, bool animate)
        {
            tint = color;
            for (int i=0;i<bubbles.Length;i++)
                bubbles[i]=PotionUI.Circle(transform,0,0,8+i%3*4,8+i%3*4,tint);
            SetMotion(animate);
            DrawBubbles();
        }
        public void SetMotion(bool animate)
        {
            moving=animate;
            foreach(Image bubble in bubbles) if(bubble!=null) bubble.gameObject.SetActive(animate);
        }
        void Update()
        {
            if(!moving)return;
            elapsed+=Time.unscaledDeltaTime;
            DrawBubbles();
        }
        void DrawBubbles()
        {
            for(int i=0;i<bubbles.Length;i++)
            {
                float progress=(elapsed*.35f+i*.137f)%1;
                bubbles[i].rectTransform.anchoredPosition=new Vector2(49+i*27,-(110-progress*98));
                Color color=tint; color.a=1-progress; bubbles[i].color=color;
            }
        }
    }
}
