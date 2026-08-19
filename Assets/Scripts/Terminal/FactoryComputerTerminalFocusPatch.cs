
using UnityEngine;

public class FactoryComputerTerminalFocusPatch : MonoBehaviour
{
    [Header("Referência")]
    public FactoryComputerTerminalFocus terminalFocus;

    [Header("Debug")]
    public bool showLogs = true;

    void Awake()
    {
        if (terminalFocus == null)
            terminalFocus = GetComponent<FactoryComputerTerminalFocus>();

        if (terminalFocus == null)
            terminalFocus = GetComponentInParent<FactoryComputerTerminalFocus>();

        if (terminalFocus == null)
            terminalFocus = GetComponentInChildren<FactoryComputerTerminalFocus>(true);
    }

    // Métodos públicos simples para o sistema de interação chamar por reflection.
    public void Interact()
    {
        TryOpenTerminal();
    }

    public void Use()
    {
        TryOpenTerminal();
    }

    public void OpenTerminal()
    {
        TryOpenTerminal();
    }

    public void UseTerminal()
    {
        TryOpenTerminal();
    }

    void TryOpenTerminal()
    {
        if (terminalFocus == null)
        {
            if (showLogs)
                Debug.LogWarning("[FactoryComputerTerminalFocusPatch] Nenhum FactoryComputerTerminalFocus encontrado.", this);
            return;
        }

        // Tenta chamar possíveis nomes de método existentes no script do terminal.
        string[] methodNames =
        {
            "OpenTerminal",
            "Open",
            "UseTerminal",
            "Interact",
            "BeginTerminalFocus",
            "EnterTerminal",
            "StartTerminal"
        };

        foreach (string methodName in methodNames)
        {
            var method = terminalFocus.GetType().GetMethod(
                methodName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic,
                null,
                System.Type.EmptyTypes,
                null
            );

            if (method != null)
            {
                if (showLogs)
                    Debug.Log("[FactoryComputerTerminalFocusPatch] Chamando método: " + methodName, terminalFocus);

                method.Invoke(terminalFocus, null);
                return;
            }
        }

        if (showLogs)
            Debug.LogWarning("[FactoryComputerTerminalFocusPatch] O script FactoryComputerTerminalFocus não possui um método de abrir reconhecido.", terminalFocus);
    }
}
