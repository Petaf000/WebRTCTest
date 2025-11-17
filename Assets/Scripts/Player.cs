using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private float speed = 10f;
    private Vector2 moveInput = Vector2.zero;

    private void Start()
    {
        
    }

    private void Update()
    {
        var move = new Vector3(moveInput.x, 0f, moveInput.y) * speed * Time.deltaTime;
        transform.Translate(move);
    }

    public void Move(Vector2 inputVal)
    {
        moveInput = inputVal;
    }

    public void Jump()
    {
        Debug.Log("Jumping");
    }
}
