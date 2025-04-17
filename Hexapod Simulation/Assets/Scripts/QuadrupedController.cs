using UnityEngine;
using System.Collections.Generic;

public enum QuadrupedGaitType
{
    Trot,   // Diagonal legs move together (FR+BL, FL+BR)
    Walk,   // One leg at a time in sequence
    Pace,   // Legs on same side move together (FR+BR, FL+BL)
    Bound   // Front legs together, back legs together
}

public class QuadrupedController : MonoBehaviour
{
    
}