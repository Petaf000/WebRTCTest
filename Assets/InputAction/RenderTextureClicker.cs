using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

// RawImageに張り付けて、クリックイベントをRenderTextureの向こう側のCanvasに転送する
public class RenderTextureClicker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerClickHandler
{
    [Header("転送先の設定")]
    [SerializeField] private Camera targetCamera;      // 例: P1_UICam
    [SerializeField] private GraphicRaycaster targetRaycaster; // 例: P1_UI_Canvas

    // RawImage上のクリック位置を、向こう側のカメラのスクリーン座標に変換する
    private Vector2 GetTargetScreenPosition(Vector2 screenPos)
    {
        // 1. クリックされたRawImage上のローカル座標を取得
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            GetComponent<RectTransform>(),
            screenPos,
            GetComponentInParent<Canvas>().worldCamera, // 自分(Sub_Canvas)を映しているカメラ
            out Vector2 localPoint
        );

        // 2. ローカル座標を 0～1 (UV座標) に正規化
        Rect rect = GetComponent<RectTransform>().rect;
        Vector2 viewportPos = new Vector2(
            (localPoint.x - rect.x) / rect.width,
            (localPoint.y - rect.y) / rect.height
        );

        // 3. 向こう側のカメラのスクリーン座標に変換
        return targetCamera.ViewportToScreenPoint(viewportPos);
    }

    // --- イベントの転送処理 ---

    public void OnPointerDown(PointerEventData eventData)
    {
        RelayEvent(eventData, ExecuteEvents.pointerDownHandler);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        RelayEvent(eventData, ExecuteEvents.pointerUpHandler);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RelayEvent(eventData, ExecuteEvents.pointerClickHandler);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RelayEvent(eventData, ExecuteEvents.dragHandler);
    }

    // 実際にイベントを向こう側のUIに投げる関数
    private void RelayEvent<T>(PointerEventData originalData, ExecuteEvents.EventFunction<T> eventFunction) where T : IEventSystemHandler
    {
        // 向こう側の世界用のポインターデータを作成
        PointerEventData relayData = new PointerEventData(EventSystem.current);
        relayData.position = GetTargetScreenPosition(originalData.position);
        relayData.button = originalData.button;

        // 向こう側のCanvasに対してRaycast（当たり判定）
        List<RaycastResult> results = new List<RaycastResult>();
        targetRaycaster.Raycast(relayData, results);

        // 何かに当たったらイベントを実行（ボタンを押すなど）
        if (results.Count > 0)
        {
            GameObject target = results[0].gameObject;
            ExecuteEvents.Execute(target, relayData, eventFunction);
        }
    }
}