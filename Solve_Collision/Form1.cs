using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Drawing.Imaging;
using System.IO;
using System.Globalization;

namespace Solve_Collision
{
    public partial class Form1 : Form
    {
        const int particleCount = 800;
        const float worldWidth = 800;
        const float worldHeight = 800f;
        const float radius = 4;       

        #region Global variables      
        static int PixelsSizeView = 800 * 800 * 4;//ARGB
        static byte[] PixelsView = new byte[PixelsSizeView];
        static object LockerPixelsView = new object();
        static Bitmap PreVisualitzacio = new Bitmap(800, 800, PixelFormat.Format32bppArgb);
        static BitmapData PreVisualitzacioData;
        static Rectangle Rectangle_PreVisualitzacioData = new Rectangle(0, 0, 640, 480);     
        #endregion

        [DllImport("winmm.dll")]
        internal static extern uint timeBeginPeriod(uint period);

        public Form1()
        {
            InitializeComponent();

            timeBeginPeriod(1);

            Thread ThreadSolve_Collision = new Thread(Solve_Collision);
            ThreadSolve_Collision.Start();                      
        }

        delegate void Render_to_pictureBox(Bitmap Image);

        void func_Render_to_pictureBox(Bitmap Image)
        {
            pictureBox_Image.Image = Image;           
        }

        //Solve_Collision & Display
        void Solve_Collision()
        {
            Render_to_pictureBox render_to_pictureBox = new Render_to_pictureBox(func_Render_to_pictureBox);

            #region Variables
            ParticleSystem system = new ParticleSystem(particleCount, worldWidth, worldHeight, radius);
            Bitmap Image = new Bitmap(800, 800, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(Image);
            Brush particleBrush = new SolidBrush(Color.Red);
            int i;
            float x, y;
            float diameter = radius * 2f;
            DateTime OldDateTime = DateTime.Now;
            TimeSpan Interval;
            float dt; //= 0.016f; 
            #endregion

            while (true)
            {
                #region Generate dt
                Interval =  DateTime.Now - OldDateTime;
                dt = (float)Interval.TotalMilliseconds / 1000f;
                OldDateTime = DateTime.Now;
                if (dt==0)
                {
                    dt = 0.001f;
                }
                #endregion
                 
                system.Update(dt);

                #region Generate Image
                g.Clear(Color.Black);

                for (i=0;i< system.Particles.Length;i++)
                {
                    x = system.Particles[i].X - radius;
                    y = system.Particles[i].Y - radius;                    
                    g.FillEllipse(particleBrush, x, y, diameter, diameter);
                }
                #endregion

                this.Invoke(render_to_pictureBox, Image);

                Thread.Sleep(1);
            }
        }

        public struct Point2D
        {
            public float X, Y;
            public float Vx, Vy;           
        }

        public class ParticleSystem
        {
            public Point2D[] Particles;
            public float WorldMinX;
            public float WorldMinY;
            public float WorldMaxX;
            public float WorldMaxY;
            public float CellSize;          
            private int gridCols, gridRows;
            private int totalCells;
            private int[] cellStart;   
            private int[] particleNext;
            private Point[] neighbors = new Point[8];
        
            public ParticleSystem(int particleCount, float worldWidth, float worldHeight, float radius)
            {
                WorldMinX = 0f;
                WorldMinY = 0f;
                WorldMaxX = worldWidth;
                WorldMaxY = worldHeight;                 
                CellSize = 2f * radius;   

                Particles = new Point2D[particleCount];
                var rnd = new Random(42);
                float margin = radius * 2f;
                float availableW = worldWidth - 2f * margin;
                float availableH = worldHeight - 2f * margin;
                int i;

                for (i = 0; i < particleCount; i++)
                {
                    Particles[i] = new Point2D();
                    Particles[i].X = margin + (float)rnd.NextDouble() * availableW;
                    Particles[i].Y = margin + (float)rnd.NextDouble() * availableH;
                    Particles[i].Vx = (float)(rnd.NextDouble() - 0.5) * 2f;   // random speed between -1 and 1
                    Particles[i].Vy = (float)(rnd.NextDouble() - 0.5) * 2f;                    
                }

                gridCols = (int)Math.Ceiling((WorldMaxX - WorldMinX) / CellSize);
                gridRows = (int)Math.Ceiling((WorldMaxY - WorldMinY) / CellSize);
                totalCells = gridCols * gridRows;
                cellStart = new int[totalCells];
                particleNext = new int[particleCount];

                neighbors = new Point[8];
                neighbors[0] = new Point(-1, -1);
                neighbors[1] = new Point(-1, 0);
                neighbors[2] = new Point(-1, 1);
                neighbors[3] = new Point(0, -1);
                neighbors[4] = new Point(0, 1);
                neighbors[5] = new Point(1, -1);
                neighbors[6] = new Point(1, 0);
                neighbors[7] = new Point(1, 1);
            }

            //
            public void BuildGrid()
            {
                #region Variables
                int i;
                int cx;
                int cy;
                int cellIdx;
                #endregion
               
                // Reset all cell heads to -1 (empty)
                for (i=0; i< cellStart.Length;i++)
                {
                    cellStart[i] = -1;
                }                

                // Build linked lists
                for (i = 0; i < Particles.Length; i++)
                {
                    cx = (int)((Particles[i].X - WorldMinX) / CellSize);
                    cy = (int)((Particles[i].Y - WorldMinY) / CellSize);
                    if (cx < 0)
                    {
                        cx = 0;
                    }
                    else if (cx > gridCols - 1)
                    {
                        cx = gridCols - 1;
                    }
                    if (cy < 0)
                    {
                        cy = 0;
                    }
                    else if (cy > gridRows - 1)
                    {
                        cy = gridRows - 1;
                    }
                    cellIdx = cy * gridCols + cx;

                    // Insert particle at the head of the cell's list
                    particleNext[i] = cellStart[cellIdx];
                    cellStart[cellIdx] = i;
                }             
            }

            // Resolve collision between two particles
            private void ResolveCollision(int a, int b)
            {               
                float dx = Particles[a].X - Particles[b].X;
                float dy = Particles[a].Y - Particles[b].Y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                if (dist < 1e-6f) return;

                float overlap = 2f * radius - dist;
                float nx = dx / dist;
                float ny = dy / dist;

                // Separate positions to avoid sticking
                float halfOverlap = overlap * 0.5f;
                Particles[a].X += nx * halfOverlap;
                Particles[a].Y += ny * halfOverlap;
                Particles[b].X -= nx * halfOverlap;
                Particles[b].Y -= ny * halfOverlap;

                // manage collision
                float vrelx = Particles[a].Vx - Particles[b].Vx;
                float vrely = Particles[a].Vy - Particles[b].Vy;
                float dot = vrelx * nx + vrely * ny;
                if (dot < 0)
                {                   
                    float imp = dot;
                    Particles[a].Vx -= imp * nx;
                    Particles[a].Vy -= imp * ny;
                    Particles[b].Vx += imp * nx;
                    Particles[b].Vy += imp * ny;
                }
            }

            // Check collisions using the spatial grid
            public void HandleCollisions()
            {
                #region Variables
                int i;
                int j;
                int cx, cy;
                int dx, dy;
                int nx, ny;
                int cellIdx;
                float dxPos, dyPos;
                float distSq;
                int i_neighbors;
                #endregion


                for (i = 0; i < Particles.Length; i++)
                {
                    cx = (int)((Particles[i].X - WorldMinX) / CellSize);
                    cy = (int)((Particles[i].Y - WorldMinY) / CellSize);

                    // Loop over neighbors
                    for (i_neighbors=0;i_neighbors<8;i_neighbors++)
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
                                // Only check pairs where j > i (avoids double checks and self-collision)
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
            }

            // 
            public void Update(float dt)
            {
                int i;

                // Euler integration and wall bounces
                for (i = 0; i < Particles.Length; i++)
                {
                    Particles[i].X += Particles[i].Vx * dt;
                    Particles[i].Y += Particles[i].Vy * dt;

                    // Simple reflection with position clamping
                    if (Particles[i].X < WorldMinX + radius)
                    {
                        Particles[i].X = WorldMinX + radius;
                        Particles[i].Vx = -Particles[i].Vx;
                    }
                    if (Particles[i].X > WorldMaxX - radius)
                    {
                        Particles[i].X = WorldMaxX - radius;
                        Particles[i].Vx = -Particles[i].Vx;
                    }
                    if (Particles[i].Y < WorldMinY + radius)
                    {
                        Particles[i].Y = WorldMinY + radius;
                        Particles[i].Vy = -Particles[i].Vy;
                    }
                    if (Particles[i].Y > WorldMaxY - radius)
                    {
                        Particles[i].Y = WorldMaxY - radius;
                        Particles[i].Vy = -Particles[i].Vy;
                    }
                }

                BuildGrid();           // rebuild grid after movement
                HandleCollisions();    // resolve overlaps and update velocities
            }
        }
    }
}
