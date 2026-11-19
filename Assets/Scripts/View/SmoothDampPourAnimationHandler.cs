using System;
using System.Collections;
using UnityEngine;
using WaterSort.Core;

namespace WaterSort.View
{
    /// <summary>
    /// 모바일 최고 수준의 부드러운 액체 이동 연출 핸들러.
    /// 소스 병의 부드러운 접근/틸트, StreamRenderer를 통한 3차 베지어 곡선 물줄기 렌더링,
    /// 그리고 타깃 병의 수면 Wobble 찰랑임 상호작용 연동.
    /// </summary>
    public class SmoothDampPourAnimationHandler : MonoBehaviour, IPourAnimationHandler
    {
        [Header("Animation Durations (Seconds)")]
        [SerializeField] private float _approachSmoothTime = 0.16f;
        [SerializeField] private float _pourDuration = 0.42f;
        [SerializeField] private float _returnSmoothTime = 0.14f;

        [Header("Pour Geometry")]
        [SerializeField] private float _pourOffsetDistance = 0.85f;
        [SerializeField] private float _pourTiltAngle = 72f;

        [Header("Stream Visual & Bezier Curve")]
        [Tooltip("3차 베지어 곡선 물줄기 렌더러")]
        [SerializeField] private StreamRenderer _streamRenderer;
        [Tooltip("레거시 직선 라인 렌더러 (폴백용)")]
        [SerializeField] private LineRenderer _streamLine;

        public IEnumerator PlayPourAnimationRoutine(
            BottleView sourceView,
            BottleView targetView,
            LiquidColor color,
            int amount,
            Action onComplete)
        {
            if (sourceView == null || targetView == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            sourceView.LogicalBottle.IsLocked = true;
            targetView.LogicalBottle.IsLocked = true;

            Vector3 sourceOrigin = sourceView.OriginalPosition;
            Quaternion sourceOriginRot = sourceView.OriginalRotation;

            // 1. 소스가 타깃의 좌측 또는 우측 중 가까운 상단으로 접근할 위치 계산
            bool pourFromLeft = sourceOrigin.x <= targetView.OriginalPosition.x;
            float sideSign = pourFromLeft ? -1f : 1f;

            Vector3 pourHoverPos = targetView.OriginalPosition + new Vector3(
                sideSign * _pourOffsetDistance,
                1.4f,
                0f
            );

            float targetAngle = pourFromLeft ? -_pourTiltAngle : _pourTiltAngle;
            Quaternion pourHoverRot = Quaternion.Euler(0, 0, targetAngle);

            // 2. 접근 및 틸트 (Approach & Tilt with SmoothDamp)
            Vector3 moveVelocity = Vector3.zero;
            float currentAngle = 0f;
            float angleVelocity = 0f;

            float approachTimer = 0f;
            while (approachTimer < _approachSmoothTime * 2.2f)
            {
                approachTimer += Time.deltaTime;
                sourceView.transform.position = Vector3.SmoothDamp(
                    sourceView.transform.position,
                    pourHoverPos,
                    ref moveVelocity,
                    _approachSmoothTime
                );

                currentAngle = Mathf.SmoothDamp(currentAngle, targetAngle, ref angleVelocity, _approachSmoothTime);
                sourceView.transform.rotation = Quaternion.Euler(0, 0, currentAngle);

                // 병을 기울이는 동안 소스 내부 수면 흔들림
                sourceView.AddWobbleImpulse(Time.deltaTime * 1.5f);

                yield return null;
            }

            sourceView.transform.position = pourHoverPos;
            sourceView.transform.rotation = pourHoverRot;

            // 3. 액체 부어지는 3차 베지어 곡선 스트림 연출 시작
            if (_streamRenderer != null)
            {
                _streamRenderer.BeginStream(sourceView.PourPoint, targetView.PourPoint, color, _pourDuration);
            }
            else if (_streamLine != null)
            {
                _streamLine.startColor = color.ColorValue;
                _streamLine.endColor = color.ColorValue;
                _streamLine.enabled = true;
                _streamLine.SetPosition(0, sourceView.PourPoint.position);
                _streamLine.SetPosition(1, targetView.PourPoint.position);
            }

            // 부어지는 시간 동안 시각적 높이 전환 및 타깃 수면 찰랑임(Wobble) 지속 트리거
            float pourTimer = 0f;
            while (pourTimer < _pourDuration)
            {
                pourTimer += Time.deltaTime;

                // 타깃 수면에 액체가 쏟아지며 발생하는 충격파(Wobble)
                targetView.AddWobbleImpulse(Time.deltaTime * 2.8f);

                if (_streamLine != null && _streamLine.enabled && _streamRenderer == null)
                {
                    _streamLine.SetPosition(0, sourceView.PourPoint.position);
                    _streamLine.SetPosition(1, targetView.PourPoint.position);
                }

                yield return null;
            }

            if (_streamRenderer != null)
            {
                _streamRenderer.StopStream();
            }
            if (_streamLine != null)
            {
                _streamLine.enabled = false;
            }

            // 양쪽 병 비주얼 최신화
            sourceView.RefreshVisuals();
            targetView.RefreshVisuals();

            // 액체가 차오른 후 타깃 병에 만족스러운 여운의 출렁임 부여
            targetView.AddWobbleImpulse(0.7f);

            // 4. 소스 병 원위치 복귀 (Return to Origin)
            Vector3 returnVelocity = Vector3.zero;
            float returnAngleVelocity = 0f;
            float returnTimer = 0f;

            while (returnTimer < _returnSmoothTime * 2.2f)
            {
                returnTimer += Time.deltaTime;
                sourceView.transform.position = Vector3.SmoothDamp(
                    sourceView.transform.position,
                    sourceOrigin,
                    ref returnVelocity,
                    _returnSmoothTime
                );

                currentAngle = Mathf.SmoothDamp(currentAngle, 0f, ref returnAngleVelocity, _returnSmoothTime);
                sourceView.transform.rotation = Quaternion.Euler(0, 0, currentAngle);

                yield return null;
            }

            sourceView.ResetTransformImmediate();

            sourceView.LogicalBottle.IsLocked = false;
            targetView.LogicalBottle.IsLocked = false;

            // 만약 타깃 병이 완성되었다면 축하 FX 발동
            if (targetView.LogicalBottle.IsCompleted)
            {
                targetView.PlayCompletionCelebration();
            }

            onComplete?.Invoke();
        }
    }
}
