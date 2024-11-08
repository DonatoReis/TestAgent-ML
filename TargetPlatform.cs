using UnityEngine;
using System.Collections;

public class TargetPlatform : MonoBehaviour
{
    [Header("Color Settings")]
    [SerializeField] private Color corNormal = Color.red;
    [SerializeField] private Color corAtivada = Color.green;

    [Header("Movement Settings")]
    [SerializeField] private float distanciaAbaixar = 0.3f;
    [SerializeField] private float velocidadeMovimento = 2f;
    [SerializeField] private float tempoEspera = 5f;

    [Header("Door Settings")]
    [SerializeField] private Door door;

    private MeshRenderer meshRenderer;
    private Vector3 posicaoInicial;
    private Vector3 posicaoAbaixada;
    private bool estaEmContato = false;
    private Coroutine movimentoCoroutine;
    private Coroutine tempoCoroutine;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip somPiso;
    [SerializeField] private AudioSource audioSource;

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogError("MeshRenderer not found! Disabling the script.");
            enabled = false;
            return;
        }

        meshRenderer.material = new Material(meshRenderer.material);
        meshRenderer.material.color = corNormal;

        posicaoInicial = transform.position;
        posicaoAbaixada = posicaoInicial - new Vector3(0, distanciaAbaixar, 0);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("albert"))
        {
            AtivaPlataforma();

            if (audioSource != null && somPiso != null)
            {
                audioSource.PlayOneShot(somPiso);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("albert"))
        {
            estaEmContato = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("albert"))
        {
            DesativaPlataforma();
        }
    }

    private void AtivaPlataforma()
    {
        if (estaEmContato) return;

        estaEmContato = true;
        StopCoroutines();

        meshRenderer.material.color = corAtivada;

        movimentoCoroutine = StartCoroutine(MoverPlataforma(transform.position, posicaoAbaixada));

        door?.OpenDoor();
    }

    private void DesativaPlataforma()
    {
        estaEmContato = false;
        tempoCoroutine = StartCoroutine(EsperarEDesativar());
    }

    private IEnumerator EsperarEDesativar()
    {
        yield return new WaitForSeconds(tempoEspera);

        if (!estaEmContato)
        {
            meshRenderer.material.color = corNormal;
            movimentoCoroutine = StartCoroutine(MoverPlataforma(transform.position, posicaoInicial));
            door?.CloseDoor();
        }
    }

    private IEnumerator MoverPlataforma(Vector3 posicaoInicio, Vector3 posicaoAlvo)
    {
        while (Vector3.Distance(transform.position, posicaoAlvo) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, posicaoAlvo, velocidadeMovimento * Time.deltaTime);
            yield return null;
        }
        transform.position = posicaoAlvo;
    }

    private void StopCoroutines()
    {
        if (movimentoCoroutine != null)
        {
            StopCoroutine(movimentoCoroutine);
            movimentoCoroutine = null;
        }

        if (tempoCoroutine != null)
        {
            StopCoroutine(tempoCoroutine);
            tempoCoroutine = null;
        }
    }
}
