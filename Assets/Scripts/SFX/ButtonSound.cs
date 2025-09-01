using UnityEngine;

public class ButtonSound : MonoBehaviour
{
    public AudioSource audioSource;  // AudioSource 연결
    public AudioClip clickSound;     // 클릭 사운드 파일

    public void PlayClickSound()
    {
        // 임시 오디오 오브젝트 생성
        GameObject tempAudioObj = new GameObject("TempAudio");
        AudioSource tempAudio = tempAudioObj.AddComponent<AudioSource>();

        tempAudio.clip = clickSound;
        tempAudio.playOnAwake = false;
        tempAudio.spatialBlend = 0f; // 2D 사운드
        tempAudio.volume = audioSource != null ? audioSource.volume : 1f; // 기존 볼륨 유지

        DontDestroyOnLoad(tempAudioObj); // 씬 전환 후에도 유지
        tempAudio.Play();

        // 소리 재생 후 오브젝트 제거
        Destroy(tempAudioObj, clickSound.length);
    }
}