using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private bool vertical = false;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float moveTime = 2f;

   
   

    private Vector2 direction;

    private void Start()
    {
        if (vertical)
            direction = Vector2.up;
        else
            direction = Vector2.right;

        StartCoroutine(MoveRoutine());
    }

    private IEnumerator MoveRoutine()
    {
        while (true)
        {
            yield return Move(moveTime);

            yield return new WaitForSeconds(0.5f);

            direction *= -1;

            yield return Move(moveTime);

            yield return new WaitForSeconds(0.5f);

            direction *= -1;
        }
    }

    private IEnumerator Move(float time)
    {
        float timer = 0f;

        while (timer < time)
        {
            transform.Translate(direction * speed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }
    }
}
