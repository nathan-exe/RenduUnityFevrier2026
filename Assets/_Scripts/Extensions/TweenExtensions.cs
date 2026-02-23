//
// using DG.Tweening;
// using UnityEngine;
//
// namespace _Scripts.Extensions
// {
//     public static class TweenExtensions
//     {
//         public static Tween DoSquashAndStretch(this Transform t,float height = 1.2f,float duration = .7f,Ease ease = Ease.OutElastic,float SplitTime = .1f)
//         {
//             Sequence s = DOTween.Sequence();
//             s.Append(t.DOScale(new Vector3(1f / height * Mathf.Sign(t.localScale.x), height, 1), duration*(SplitTime)).SetEase(Ease.OutSine));
//             s.Append(t.DOScale(new Vector3(Mathf.Sign(t.localScale.x),1,1), duration*(1-SplitTime)).SetEase(ease));
//             return s;
//         }
//
//         public static Tween DoColor(this SpriteRenderer sprite, Color endColor, float duration)
//         {
//             Tween t
//                 = DOTween.To(
//                     () => sprite.color,
//                     (Color x) => sprite.color = x,
//                     endColor,
//                     duration);
//             
//             return t;
//         } 
//         
//         public static Tween DoFade(this SpriteRenderer sprite, float endAlpha, float duration)
//         {
//             Tween t
//                 = DOTween.To(
//                     () => sprite.color.a,
//                     (float a) => sprite.color = new Color(sprite.color.r,sprite.color.g,sprite.color.b,a),
//                     endAlpha,
//                     duration);
//             
//             return t;
//         } 
//         
//         public static Tween DoBlinkColor(this SpriteRenderer sprite, Color blinkColor,Color endColor, float duration)
//         {
//             Color startColor = sprite.color;
//             Sequence s = DOTween.Sequence();
//             s.Append(sprite.DoColor(blinkColor, duration * .3f)).SetEase(Ease.OutExpo);
//             s.Append(sprite.DoColor(endColor, duration * .7f)).SetEase(Ease.OutExpo);
//             return s;
//         } 
//         
//         public static Tween DoFade(this CanvasGroup canvasGroup, float endValue, float duration)
//         {
//             Tween t
//                 = DOTween.To(
//                     () => canvasGroup.alpha,
//                     (float x) => canvasGroup.alpha = x,
//                     endValue,
//                     duration);
//             
//             return t;
//         } 
//     }
//     
//     
// }