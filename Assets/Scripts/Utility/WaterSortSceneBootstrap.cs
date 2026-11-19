using UnityEngine;
using UnityEngine.UI;
using WaterSort.Core;
using WaterSort.Level;
using WaterSort.UI;
using WaterSort.View;

namespace WaterSort.Utility
{
    /// <summary>
    /// 개발 및 테스트용 원클릭 씬 부트스트랩 컴포넌트.
    /// 에디터에서 빈 씬에 이 스크립트만 붙이고 플레이하면
    /// GameManager, DifficultyManager, LevelGenerator, 모던 파스텔 UI 캔버스를 자동으로 구성합니다.
    /// </summary>
    public class WaterSortSceneBootstrap : MonoBehaviour
    {
        [Header("Auto Setup on Start")]
        [SerializeField] private bool _autoSetup = true;

        private void Awake()
        {
            if (_autoSetup)
            {
                SetupSystems();
            }
        }

        private void SetupSystems()
        {
            // 1. 카메라 배경 설정 (모던 소프트 파스텔 배경)
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = UITheme.BackgroundLight;
                mainCam.orthographic = true;
                mainCam.orthographicSize = 6.0f;
            }

            // 2. DifficultyManager 보장
            if (DifficultyManager.Instance == null)
            {
                GameObject dmObj = new GameObject("[DifficultyManager]");
                dmObj.AddComponent<DifficultyManager>();
            }

            // 3. LevelGenerator 보장
            if (LevelGenerator.Instance == null)
            {
                GameObject lgObj = new GameObject("[LevelGenerator]");
                lgObj.AddComponent<LevelGenerator>();
            }

            // 4. Animation Handler 보장
            SmoothDampPourAnimationHandler animHandler = FindObjectOfType<SmoothDampPourAnimationHandler>();
            if (animHandler == null)
            {
                GameObject animObj = new GameObject("[AnimationHandler]");
                animHandler = animObj.AddComponent<SmoothDampPourAnimationHandler>();
            }

            // 5. Board Container & GameManager
            if (WaterSortGameManager.Instance == null)
            {
                GameObject gmObj = new GameObject("[GameManager]");
                WaterSortGameManager gm = gmObj.AddComponent<WaterSortGameManager>();

                GameObject boardObj = new GameObject("BoardContainer");
                boardObj.transform.position = Vector3.zero;

                // 필드 리플렉션 대신 인스펙터나 런타임 연결
            }
        }
    }
}
