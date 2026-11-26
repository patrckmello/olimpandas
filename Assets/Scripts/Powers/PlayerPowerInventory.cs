using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPowerInventory : MonoBehaviour
{
    public enum PowerType
    {
        None,
        Stun,
        Slow
    }

    [SerializeField]
    private PowerType currentPower = PowerType.None;

    public PowerType CurrentPower => currentPower;

    public bool HasPower(PowerType power)
    {
        return currentPower == power;
    }

    public void GrantPower(PowerType power)
    {
        // Cada prova declara os poderes permitidos no MinigameCatalog.
        if (!MinigameCatalog.AllowsPower(SceneManager.GetActiveScene().name, power))
            return;

        currentPower = power;

        Debug.Log(
            $"{name} pegou o poder {power}!"
        );
    }

    public void ConsumePower()
    {
        currentPower = PowerType.None;
    }
}