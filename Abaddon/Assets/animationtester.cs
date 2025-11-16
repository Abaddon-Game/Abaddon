using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AnimationController))]
public class animationtester : MonoBehaviour
{
    private AnimationController animationManager;
    private bool isPlayer = true;
    Animation Other;

    readonly Animation Ghost = new Animation(
        name: "Ghost",
        priority: 0,
        onComplete: () => Debug.Log("Ghost completed"),
        onStart: () => Debug.Log("Ghost started")
    );

    void Start()
    {
        animationManager = GetComponent<AnimationController>();
        animationManager.setDefaultAnimation(Ghost);
        Other = new Animation(
            nameFunc: () => isPlayer ? "Cyclops" : "Player",
            priority: 2
        );
    }

    // Update is called once per frame
    void Update()
    {
        animationManager.play(Other);

        if (Input.GetKeyDown(KeyCode.T))
        {
            isPlayer = !isPlayer;
        }
    }
}
