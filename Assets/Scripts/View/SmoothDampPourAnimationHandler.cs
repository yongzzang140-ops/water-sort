using System;
using System.Collections;
using UnityEngine;
using WaterSort.Core;

namespace WaterSort.View
{
    /// <summary>
    /// 외부 서드파티 플러그인 종속성 없이, Unity 내장 Mathf.SmoothDamp 및 베지어 곡선 보간을 활용한
    /// 모바일 최고 수준의 부드러운 액체 이동 연출 핸들러.
    /// </summary>
    public class SmoothDampPourAnimationHandler : MonoBehaviour, IPourAnimationHandler
    {
        [Header("Animation Durations (Seconds)")]
        [SerializeField] private float _approachSmoothTime = 0.16f;
        [SerializeField] private float _pourDuration = 0.40f;
        [SerializeField] private float _returnSmoothTime = 0.14f;

        [Header("Pour Geometry")]
        [SerializeField] private float _pourOffsetDistance = 0.85f;
        [SerializeField] private float _pourTiltAngle = 72f;

        [Header("Stream Visual")]
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

                yield return null;
            }

            sourceView.transform.position = pourHoverPos;
            sourceView.transform.rotation = pourHoverRot;

            // 3. 액체 부어지는 스트림 및 볼륨 전환 연출
            if (_streamLine != null)
            {
                _streamLine.startColor = color.ColorValue;
                _streamLine.endColor = color.ColorValue;
                _streamLine.enabled = true;
                _streamLine.SetPosition(0, sourceView.PourPoint.position);
                _streamLine.SetPosition(1, targetView.PourPoint.position);
            }

            float pourTimer = 0f;
            while (pourTimer < _pourDuration)
            {
                pourTimer += Time.deltaTime;
                if (_streamLine != null && _streamLine.enabled)
                {
                    _streamLine.SetPosition(0, sourceView.PourPoint.position);
                    _streamLine.SetPosition(1, targetView.PourPoint.position);
                }
                yield return null;
            }

            if (_streamLine != null)
            {
                _streamLine.enabled = false;
            }

            // 비주얼 레이어 실시간 갱신
            sourceView.RefreshVisuals();
            targetView.RefreshVisuals();

            // 4. 원래 위치 및 회전 복귀 (Return with SmoothDamp)
            float returnTimer = 0f;
            Vector3 returnVelocity = Vector3.zero;
            while (returnTimer < _returnSmoothTime * 2.2f)
            {
                returnTimer += Time.deltaTime;
                sourceView.transform.position = Vector3.SmoothDamp(
                    sourceView.transform.position,
                    sourceOrigin,
                    ref returnVelocity,
                    _returnSmoothTime
                );

                currentAngle = Mathf.SmoothDamp(currentAngle, 0f, ref angleVelocity, _returnSmoothTime);
                sourceView.transform.rotation = Quaternion.Euler(0, 0, currentAngle);

                yield return null;
            }

            sourceView.ResetTransformImmediate();

            sourceView.LogicalBottle.IsLocked = false;
            targetView.LogicalBottle.IsLocked = false;

            // 완성 체크 시 파티클
            if (targetView.LogicalBottle.IsCompletedSingleColor)
            {
                PlayCompletionEffect(targetView);
            }

            onComplete?.Invoke();
        }

        public void PlayCompletionEffect(BottleView bottleView)
        {
            if (bottleView != null)
            {
                bottleView.PlayCompletionCelebration();
            }
        }
    }
}
