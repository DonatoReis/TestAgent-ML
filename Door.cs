using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float alturaAbertura = 4f;
    [SerializeField] private float velocidadeMovimento = 5f;

    private Vector3 posicaoFechada;
    private Vector3 posicaoAberta;
    private Coroutine movimentoCoroutine;

    private void Start()
    {
        posicaoFechada = transform.position;
        posicaoAberta = posicaoFechada + Vector3.up * alturaAbertura;
    }

    public void OpenDoor()
    {
        MoverPara(posicaoAberta);
    }

    public void CloseDoor()
    {
        MoverPara(posicaoFechada);
    }

    private void MoverPara(Vector3 posicaoAlvo)
    {
        if (movimentoCoroutine != null)
        {
            StopCoroutine(movimentoCoroutine);
        }
        movimentoCoroutine = StartCoroutine(MoverPorta(posicaoAlvo));
    }

    private IEnumerator MoverPorta(Vector3 posicaoAlvo)
    {
        while (Vector3.Distance(transform.position, posicaoAlvo) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, posicaoAlvo, velocidadeMovimento * Time.deltaTime);
            yield return null;
        }

        transform.position = posicaoAlvo;
    }
}
