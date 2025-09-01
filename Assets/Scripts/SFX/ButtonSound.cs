using UnityEngine;

public class ButtonSound : MonoBehaviour
{
    public AudioSource audioSource;  // AudioSource ����
    public AudioClip clickSound;     // Ŭ�� ���� ����

    public void PlayClickSound()
    {
        // �ӽ� ����� ������Ʈ ����
        GameObject tempAudioObj = new GameObject("TempAudio");
        AudioSource tempAudio = tempAudioObj.AddComponent<AudioSource>();

        tempAudio.clip = clickSound;
        tempAudio.playOnAwake = false;
        tempAudio.spatialBlend = 0f; // 2D ����
        tempAudio.volume = audioSource != null ? audioSource.volume : 1f; // ���� ���� ����

        DontDestroyOnLoad(tempAudioObj); // �� ��ȯ �Ŀ��� ����
        tempAudio.Play();

        // �Ҹ� ��� �� ������Ʈ ����
        Destroy(tempAudioObj, clickSound.length);
    }
}