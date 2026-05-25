# Particle Collision Simulator

This **C# / Visual Studio 2017** project simulates **800 particles** of fixed radius **4 pixels** moving inside an **800×800 pixel** arena.  
Collisions are detected using a neighbor spatial grid and resolved by directly reversing the relative velocity along the collision normal.

## Features

- **800 particles** – each with position (x, y) and velocity (vx, vy)
- **Fixed radius** – 4 pixels (diameter 8 pixels)
- **800×800 display** – perfect square simulation area

## Requirements

- Visual Studio 2017 
- Windows 7/8/10/11

## Getting Started

1. Clone the repository:
2. Open the project:
   - Launch Visual Studio 2017.
   - Open the `.sln` solution file.
3. **Run the application**:
   - Press `F5` or click **Start** to build and launch the viewer.

## Description
The application initialises 800 particles with random positions and velocities.  
Each frame, each particle checks for overlaps only with the particles located in its own grid cell and the surrounding cells. This spatial partitioning avoids the expensive O(N²) all‑pairs check.  
When a collision is found, the velocities are updated.

The result is a colourful, constantly moving swarm that exhibits realistic bounce behaviour.

## Collision Detection Algorithm
A uniform grid covers the simulation area. The cell size is chosen so that any particle can only collide with particles in its own cell or the eight immediately adjacent cells.
```csharp
for (i = 0; i < Particles.Length; i++)
{
    cx = (int)((Particles[i].X - WorldMinX) / CellSize);
    cy = (int)((Particles[i].Y - WorldMinY) / CellSize);

    // Loop over neighbours (including the cell itself)
    for (i_neighbors = 0; i_neighbors < 9; i_neighbors++)
    {
        nx = cx + neighbors[i_neighbors].X;
        ny = cy + neighbors[i_neighbors].Y;

        // Only proceed if neighbour cell is inside the grid
        if ((nx >= 0) && (nx < gridCols) &&
            (ny >= 0) && (ny < gridRows))
        {
            cellIdx = ny * gridCols + nx;

            // Walk the linked list of particles in this cell
            for (j = cellStart[cellIdx]; j != -1; j = particleNext[j])
            {
                if (j > i)
                {
                    dxPos = Particles[i].X - Particles[j].X;
                    dyPos = Particles[i].Y - Particles[j].Y;
                    distSq = dxPos * dxPos + dyPos * dyPos;
                    if (distSq < (2f * radius) * (2f * radius))
                    {
                        ResolveCollision(i, j);
                    }
                }
            }
        }
    }
}
```

## Collision Resolution Mechanism
When two particles collide, their velocities must change to simulate a realistic bounce. The algorithm uses a simple impulse method that works directly with velocity vectors – no angles, no masses, no trigonometry.
```csharp
// Collision normal (unit vector from b to a)
float nx = ...; // normalised x component
float ny = ...; // normalised y component

// Relative velocity
float vrelx = Particles[a].Vx - Particles[b].Vx;
float vrely = Particles[a].Vy - Particles[b].Vy;

// Projection onto the normal
float dot = vrelx * nx + vrely * ny;

if (dot < 0)   // Particles are moving towards each other
{
    float imp = dot;
    Particles[a].Vx -= imp * nx;
    Particles[a].Vy -= imp * ny;
    Particles[b].Vx += imp * nx;
    Particles[b].Vy += imp * ny;
}
```
