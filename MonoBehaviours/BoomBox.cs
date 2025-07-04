using MidiPlayerTK;
using UnityEngine;

namespace BBPlusCustomMusics.MonoBehaviours;

public class BoomBox : MonoBehaviour
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
        transform.localScale += ((one * minLimit) - transform.localScale) * 3.8f * Time.unscaledDeltaTime;
        if (transform.localScale.y < minLimit)
            transform.localScale = one * minLimit;
        if (transform.localScale.y > maxLimit)
            transform.localScale = one * maxLimit;

        axisOffset += (1f - axisOffset) * 3.4f * Time.unscaledDeltaTime;
        transform.rotation = Quaternion.Euler(0f, 0f, axisOffset);
    }

    float axisOffset = 0f;

    const float maxLimit = 0.65f, minLimit = 0.5f, axisLimit = 15f, incrementConstant = 0.65f;

    Vector3 one = Vector3.one;

}