using System;
using UnityEngine;

// Pescador: a vara fica fixa e o jogador só digita as setas. A primeira seta
// escolhe o peixe; errar a sequência solta o peixe e trava por um instante.
// O PlayerController fica desligado; ele só fornece as teclas e o personagem.
[RequireComponent(typeof(PlayerController))]
public class FishingPlayer : MonoBehaviour
{
    [Header("Controles")]
    [SerializeField] private KeyCode upKey = KeyCode.W;
    [SerializeField] private KeyCode downKey = KeyCode.S;

    [Header("Erro")]
    [SerializeField] private float missLockout = 0.5f;
    [Tooltip("Pausa sem punição quando o peixe some no meio da sequência.")]
    [SerializeField] private float lostGrace = 0.25f;

    [Header("Vara (fixa)")]
    [SerializeField] private Transform rodTip;
    [SerializeField] private Transform bobber;
    [SerializeField] private float bobberSpeed = 18f;
    [SerializeField] private LineRenderer line;

    public event Action Missed;

    public PlayerController Player => player;
    public int PlayerId => player.PlayerId;
    public RiverFish Target { get; private set; }
    public int Progress { get; private set; }
    public bool IsLocked => Time.time < lockedUntil;
    public Vector3 CatchPoint => rodTip != null ? rodTip.position : transform.position;

    private PlayerController player;
    private FishingManager manager;
    private bool fishing;
    private float lockedUntil;
    private RiverFish reeling;
    private Vector3 bobberHome;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        // Vista de cima: não há chão, o pescador não pode cair.
        GetComponent<Rigidbody2D>().gravityScale = 0f;
        if (bobber != null)
            bobberHome = bobber.position;
    }

    public void Begin(FishingManager owner)
    {
        manager = owner;
        lockedUntil = 0f;
        fishing = true;
    }

    public void Stop()
    {
        Release();
        fishing = false;
    }

    public void Press(FishArrow arrow)
    {
        if (!fishing || IsLocked)
            return;

        // Sequência pronta: espera o manager confirmar a captura neste quadro.
        if (Target != null && Progress >= Target.Sequence.Count)
            return;

        if (Target == null)
        {
            Target = manager.FindFish(arrow);
            Progress = 0;
        }

        if (Target == null || Target.Sequence[Progress] != arrow)
        {
            Miss();
            return;
        }

        Progress++;
        Target.SetProgress(PlayerId, Progress);

        if (Progress >= Target.Sequence.Count)
            manager.Complete(this, Target);
    }

    // Peixe pescado vem com a linha e a boia até a ponta da vara.
    public void Reel(RiverFish fish)
    {
        reeling = fish;
        fish.CatchTo(CatchPoint);
    }

    // Outro jogador pegou o peixe ou ele foi embora: solta sem punir.
    public void LoseTarget()
    {
        Release();
        lockedUntil = Mathf.Max(lockedUntil, Time.time + lostGrace);
    }

    public void Release()
    {
        if (Target != null)
            Target.SetProgress(PlayerId, 0);
        Target = null;
        Progress = 0;
    }

    private void Miss()
    {
        Release();
        lockedUntil = Time.time + missLockout;
        Missed?.Invoke();
    }

    private void Update()
    {
        if (!fishing)
            return;

        if (Input.GetKeyDown(upKey)) Press(FishArrow.Up);
        if (Input.GetKeyDown(downKey)) Press(FishArrow.Down);
        if (Input.GetKeyDown(player.LeftKey)) Press(FishArrow.Left);
        if (Input.GetKeyDown(player.RightKey)) Press(FishArrow.Right);
    }

    private void LateUpdate()
    {
        if (line == null || rodTip == null || bobber == null)
            return;

        // A boia vai até o peixe fisgado, volta com ele e depois retorna ao lugar.
        RiverFish hooked = Target != null ? Target : reeling;
        if (hooked == null)
            bobber.position = Vector3.MoveTowards(bobber.position, bobberHome, bobberSpeed * Time.deltaTime);
        else if (hooked.IsCaught)
            bobber.position = hooked.HookPoint;
        else
            bobber.position = Vector3.MoveTowards(bobber.position, hooked.HookPoint, bobberSpeed * Time.deltaTime);

        line.SetPosition(0, rodTip.position);
        line.SetPosition(1, bobber.position);
    }
}
