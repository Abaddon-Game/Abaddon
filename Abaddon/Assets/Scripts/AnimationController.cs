using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationController : MonoBehaviour
{
    private Animator animator;
    private Animation queuedAnimation = null;
    private CurrentAnimation currentAnimation = new CurrentAnimation();
    private Animation defaultAnimation;

    void Start()
    {
        animator = GetComponent<Animator>();
        queuedAnimation = defaultAnimation;
    }

    public void play(Animation animation) => queueAnimation(animation);
    private void queueAnimation(Animation animation)
    {
        if (animation.priority >= (queuedAnimation?.priority ?? defaultAnimation?.priority ?? int.MinValue))
        {
            print("queueAnimation");
            queuedAnimation = animation;
        }
    }

    public void stop()
    {
        if (currentAnimation != null)
        {
            currentAnimation.onStopped?.Invoke();
            currentAnimation = null;
        }
    }
    public void setDefaultAnimation(Animation animation)
    {
        defaultAnimation = animation;
        queueAnimation(animation);
    }

    void Update()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (currentAnimation.timeRunning >= stateInfo.length && currentAnimation.shouldSwapTo(defaultAnimation))
        {
            currentAnimation.complete();
        }

        // If there is a queued animation and it is different from the current animation or allows overriding itself...
        if (currentAnimation.shouldSwapTo(queuedAnimation))
        {
            //... play it 
            print("Swapping to " + queuedAnimation.name);
            currentAnimation.start(queuedAnimation);
            animator.Play(queuedAnimation.name);
        }
        else if (currentAnimation.isEmpty)
        {
            currentAnimation.start(defaultAnimation);
            animator.Play(defaultAnimation.name);
        }
        queuedAnimation = null;
    }
}

public class CurrentAnimation : Animation
{
    private float timeStarted;
    public float timeRunning => Time.time - timeStarted;
    public bool isEmpty { get; private set; } = false;

    public CurrentAnimation() : base(name: null, -1, null, null, null, null)
    {
        timeStarted = Mathf.Infinity;
        isEmpty = true;
    }

    public void start(Animation anim)
    {
        onStopped?.Invoke();

        isEmpty = false;
        timeStarted = Time.time;

        nameFunc = anim.nameFunc;
        priority = anim.priority;
        animationEvents = anim.animationEvents;
        onStart = anim.onStart;
        onComplete = anim.onComplete;
        onStopped = anim.onStopped;

        anim.onStart?.Invoke();
    }

    public void stop()
    {
        isEmpty = true;
        onStopped?.Invoke();
        timeStarted = Mathf.Infinity;
    }

    public void complete()
    {
        isEmpty = true;
        onComplete?.Invoke();
        timeStarted = Mathf.Infinity;
    }

    public bool shouldSwapTo(Animation anim)
    {
        return anim != null && (isEmpty || anim.overrideSelf || !anim.equals(this));
    }
}

public class Animation
{
    public Func<string> nameFunc;
    public string name => nameFunc();
    public int priority;

    public Action onComplete, onStart, onStopped;
    public Dictionary<float, Action> animationEvents;

    // Special Flags
    public bool overrideSelf;


    public Animation(string name, int priority = 0, Dictionary<float, Action> animationEvents = null, Action onStart = null, Action onComplete = null, Action onStopped = null, bool overrideSelf = false)
    {
        nameFunc = () => name;
        this.priority = priority;
        this.animationEvents = animationEvents ?? new Dictionary<float, Action>();
        this.onStart = onStart;
        this.onStopped = onStopped;
        this.onComplete = onComplete;
        this.overrideSelf = overrideSelf;
    }

    public Animation(Func<string> nameFunc, int priority = 0, Dictionary<float, Action> animationEvents = null, Action onStart = null, Action onComplete = null, Action onStopped = null, bool overrideSelf = false)
    {
        this.nameFunc = nameFunc;
        this.priority = priority;
        this.animationEvents = animationEvents ?? new Dictionary<float, Action>();
        this.onStart = onStart;
        this.onStopped = onStopped;
        this.onComplete = onComplete;
        this.overrideSelf = overrideSelf;
    }

    public bool equals(Animation other)
    {
        if (other == null) return false;

        return name == other.name;
    }
}