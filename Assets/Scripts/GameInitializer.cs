using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    void Awake()
    {
        // 게임이 시작될 때 저장된 오프셋 값을 불러옵니다.
        GameSettings.LoadSettings();
    }
}
