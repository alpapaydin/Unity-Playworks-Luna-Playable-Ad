using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private AudioClip thudSound;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip popSound;
    private Animator m_Animator;
    private AudioSource m_AudioSource;
    private int endCardHash = Animator.StringToHash("endCard");

    private void Start()
    {
        m_Animator = GetComponent<Animator>();
        m_AudioSource = gameObject.AddComponent<AudioSource>();
    }
    public void CallToAction()
    {
        PlayPop();
        Luna.Unity.Playable.InstallFullGame();
    }
    public void PlayThud() { m_AudioSource.PlayOneShot(thudSound); }
    public void PlayWin() { m_AudioSource.PlayOneShot(winSound); }
    public void PlayPop() { m_AudioSource.PlayOneShot(popSound); }

    public void ShowEndCard() { m_Animator.SetTrigger(endCardHash); }
    public void EndCardPresented() { Luna.Unity.LifeCycle.GameEnded(); }

}
