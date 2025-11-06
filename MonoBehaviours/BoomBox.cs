using BBPlusCustomMusics.Patches;
using MidiPlayerTK;
using UnityEngine;

namespace BBPlusCustomMusics.MonoBehaviours;

public class BoomBox : MenuButton
{
    void MidiEvent(MPTKEvent midiEvent)
    {
        if (midiEvent.Command == MPTKCommand.NoteOn || midiEvent.Command == MPTKCommand.NoteOff)
        {
            transform.localScale += midiEvent.Value * incrementConstant * one;
            if (transform.localScale.y > maxLimit)
                transform.localScale = one * maxLimit;
            axisOffset += midiEvent.Value * Random.Range(-1, 2);
            axisOffset = Mathf.Clamp(axisOffset, -axisLimit, axisLimit);
        }

    }

    void OnEnable() =>
        MusicManager.OnMidiEvent += MidiEvent;

    void OnDisable() =>
        MusicManager.OnMidiEvent -= MidiEvent;


    void Update()
    {
        scale += ((one * minLimit) - scale) * 3.8f * Time.unscaledDeltaTime;
        if (scale.y < minLimit)
            scale = one * minLimit;
        if (scale.y > maxLimit)
            scale = one * maxLimit;

        transform.localScale = scale * currentSize;

        axisOffset += (1f - axisOffset) * 3.4f * Time.unscaledDeltaTime;
        transform.rotation = Quaternion.Euler(0f, 0f, axisOffset);

        // StandardMenuButton workaround implemented here (why didn't you just add a normal Unhighlight function in MenuButton, mystman??)
        if (!highlighted && wasHighlighted)
            Unhighlight();
        highlighted = false;
    }
    // ******* Menu Button stuff ********
    public override void Highlight()
    {
        base.Highlight();
        currentSize = highlightSize;
        highlighted = true;
        wasHighlighted = true;
    }

    public void Unhighlight()
    {
        currentSize = 1f;
        highlighted = false;
        wasHighlighted = false;
    }

    public override void Press() =>
        Singleton<MusicManager>.Instance.PlayMidi(ElevatorCustomMusicPatch.GetNewRandomElevatorMidi(), loop: true);


    float axisOffset = 0f, currentSize = 1f;
    Vector3 scale = Vector3.one;

    [SerializeField]
    internal float maxLimit = 0.65f, minLimit = 0.5f, axisLimit = 15f, incrementConstant = 0.65f, highlightSize = 1.15f;

    Vector3 one = Vector3.one;

}