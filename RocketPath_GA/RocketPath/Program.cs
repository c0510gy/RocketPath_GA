using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RocketPath
{
    class Program
    {
        [DllImport("kernel32.dll", EntryPoint = "GetConsoleWindow", SetLastError = true)]
        private static extern IntPtr GetConsoleHandle();
        static IntPtr handler = GetConsoleHandle();

        static void Main(string[] args)
        {
            RocketGene rg = new RocketGene(20, 5, 800, 400);

            while (true)
            {
                rg.CalNextGene(handler);
                Console.Write("\r" + rg.gen + "세대 : " + rg.BestScore + "                        ");
                //Thread.Sleep(100);
                if (rg.BestScore == 0) return;
            }
        }
    }

    class RocketGene
    {
        public class Vector
        {
            public float X;
            public float Y;
            public Vector(float X, float Y)
            {
                this.X = X;
                this.Y = Y;
            }
        }
        public class Obstacle
        {
            public float X;
            public float Y; //장애물의 맨 위 좌표
            public float H; //장애물의 높이
            public Obstacle(float X, float Y, float H)
            {
                this.X = X;
                this.Y = Y;
                this.H = H;
            }
        }
        public int[,] dnas;
        public int Ngenes = 500; //유전자 개수
        public int Ndna; //DNA 개수
        public Obstacle[] obs; //장애물
        public int Nobs; //장애물 개수
        public int width, height; //공간 크기
        public Vector start; //출발지점
        public Vector goal; //도착지점
        public int[] Scores; //각 DNA의 점수
        public int BestScore; //최고 점수
        public int gen = 0; //세대 수
        Bitmap visual; //시각화

        public const int INF = 1000000000;

        Bitmap rocket = new Bitmap(RocketPath.Properties.Resources.missile);

        Random rnd = new Random((int)DateTime.Now.Ticks);

        public RocketGene(int Ndna, int Nobs, int width, int height)
        {
            rocket.RotateFlip(RotateFlipType.Rotate90FlipNone);

            this.Ndna = Ndna;
            this.Nobs = Nobs;
            this.width = width;
            this.height = height;
            gen = 0;
           
            start = new Vector(100, height / 2);
            goal = new Vector(this.width - 100, height / 2);

            obs = new Obstacle[this.Nobs];
            //장애물 생성
            for(int j = 0; j < this.Nobs; j++)
            {
                obs[j] = new Obstacle(rnd.Next(this.width), rnd.Next(this.height - 100), rnd.Next(100, this.height / 2));
            }

            //DNA 초기화
            //DNA 구조 : 
            //로켓의 반시계방향 회전 : 1
            //로켓의 시계방향 회전 : 2
            //회전 안함 : 0
            dnas = new int[this.Ndna, Ngenes];
            for(int j = 0; j < this.Ndna; j++)
            {
                for(int q = 0; q < Ngenes; q++)
                {
                    dnas[j, q] = rnd.Next(3);
                }
            }
            BestScore = INF;

            Scores = new int[Ndna];
            visual = new Bitmap(width, height);
        }

        //다음 세대 계산
        public void CalNextGene(IntPtr handler)
        {
            gen++;

            double[] RocketsAngle = new double[Ndna]; //각 로켓의 운동 각도 (+x 축 기준 반시계 방향이 양의 각도)
            for (int j = 0; j < Ndna; j++)
                RocketsAngle[j] = 0;
            double rocketVelocity = 2;
            Vector[] Rockets = new Vector[Ndna];
            for (int j = 0; j < Ndna; j++)
                Rockets[j] = new Vector(start.X, start.Y);

            Font font = new Font("나눔고딕", 13);

            //프레임 저장
            System.IO.Directory.CreateDirectory(@"datas\" + gen);

            for (int t = 0; t < Ngenes; t++)
            {
                Graphics g = Graphics.FromImage(visual);
                g.Clear(Color.FromArgb(255, 226, 235, 243)); // 배경 설정.

                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                //시작, 끝 지점 그리기
                g.FillEllipse(new SolidBrush(Color.Red), start.X - 5, start.Y - 5, 10, 10);
                g.DrawString("출발 위치", font, new SolidBrush(Color.Black), start.X - 30, start.Y + 10);
                g.FillEllipse(new SolidBrush(Color.Green), goal.X - 5, goal.Y - 5, 10, 10);
                g.DrawString("도착 위치", font, new SolidBrush(Color.Black), goal.X - 30, goal.Y + 10);
                //장애물 그리기
                for (int j = 0; j < Nobs; j++)
                {
                    g.DrawLine(new Pen(Color.Black, 5), obs[j].X, obs[j].Y, obs[j].X, obs[j].Y + obs[j].H);
                }


                for (int j = 0; j < Ndna; j++)
                {
                    if(dnas[j, t] == 1) //+angle
                    {
                        RocketsAngle[j] += 0.05;
                    }
                    else if(dnas[j, t] == 2)
                    {
                        RocketsAngle[j] -= 0.05;
                    }

                    double dx = rocketVelocity * Math.Cos(RocketsAngle[j]);
                    double dy = rocketVelocity * Math.Sin(RocketsAngle[j]);
                    if(!Collide(Rockets[j], Convert.ToInt32(dx), Convert.ToInt32(dy))
                        && Rockets[j] != goal) //목표지점에 도착한 경우
                    {
                        Rockets[j].X += Convert.ToInt32(dx);
                        Rockets[j].Y += Convert.ToInt32(dy);
                    }

                    g.DrawImage((Image)RotateImage(rocket, (float)RocketsAngle[j]), Rockets[j].X - 15, Rockets[j].Y - 15, 30, 30);
                }

                
                /*
                if(BestScore < 3000)
                {
                    using (var graphics = Graphics.FromHwnd(handler))
                    using (var image = (Image)(new Bitmap(visual)))
                    {
                        //graphics.Clear(Color.Black);
                        graphics.DrawImage(image, 50, 50, image.Width, image.Height);
                    }
                }
                */
                
                //프레임 저장
                visual.Save(@"datas\" + gen + @"\" + t + ".png", System.Drawing.Imaging.ImageFormat.Png);
            }

            string scoreinfo = ""; //각 dna 점수 저장

            //점수 계산 (점수가 낮을 수록 유리한 DNA)
            for(int j = 0; j < Ndna; j++)
            {
                int tmp_score = 0;
                tmp_score += (int)((Rockets[j].X - goal.X) * (Rockets[j].X - goal.X));
                tmp_score += (int)((Rockets[j].Y - goal.Y) * (Rockets[j].Y - goal.Y));
                Scores[j] = tmp_score;
                if (Scores[j] < BestScore) BestScore = Scores[j];
                scoreinfo += Scores[j] + ",";
            }

            //세대 정보 저장
            System.IO.File.WriteAllText(@"datas\" + gen + @"\score_info.txt", scoreinfo);

            //Crossover
            int[] Indexer = new int[Ndna];
            bool[] chk = new bool[Ndna];
            for (int j = 0; j < Ndna; j++)
                chk[j] = false;
            
            for (int j = 0; j < Ndna; j++)
            {
                int b_v = INF, b_i = 0;
                for (int u = 0; u < Ndna; u++)
                {
                    if (!chk[u] && b_v >= Scores[u])
                    {
                        b_v = Scores[u];
                        b_i = u;
                    }
                }
                Indexer[j] = b_i;
                chk[b_i] = true;
            }

            //세대 정보 저장 (현재 최대 점수 DNA, 현재 최대 점수, 지금까지 최대 점수)
            System.IO.File.WriteAllText(@"datas\" + gen + @"\best_score_info.txt", Indexer[0] + "," + Scores[Indexer[0]] + "," + BestScore);

            for (int j = 0; j < Ndna / 2; j++)
            {
                for(int q = 0; q < Ngenes; q++)
                {
                    if(rnd.Next(100) >= 95) //돌연변이 5%
                    {
                        dnas[Indexer[Ndna - 1 - j], q] = rnd.Next(5);
                    }
                    else
                    {
                        if(rnd.Next(Scores[j] + Scores[j + 1]) <= Scores[j])
                        {
                            dnas[Indexer[Ndna - 1 - j], q] = dnas[Indexer[j], q];
                        }
                        else
                        {
                            dnas[Indexer[Ndna - 1 - j], q] = dnas[Indexer[j + 1], q];
                        }
                    }
                }
            }


        }

        //bitmap 회전
        private Bitmap RotateImage(Bitmap bmp, float angle)
        {
            angle = angle / (float)Math.PI * 180;
            Bitmap rotatedImage = new Bitmap(bmp.Width, bmp.Height);
            using (Graphics g = Graphics.FromImage(rotatedImage))
            {
                // Set the rotation point to the center in the matrix
                g.TranslateTransform(bmp.Width / 2, bmp.Height / 2);
                // Rotate
                g.RotateTransform(angle);
                // Restore rotation point in the matrix
                g.TranslateTransform(-bmp.Width / 2, -bmp.Height / 2);
                // Draw the image on the bitmap
                g.DrawImage(bmp, new Point(0, 0));
            }

            return rotatedImage;
        }

        //장애물 충돌 여부
        private bool Collide(Vector p, int dx, int dy)
        {
            //장애물 충돌 여부
            for(int q = 0; q < Nobs; q++)
            {
                if (p.X <= obs[q].X && p.X + dx >= obs[q].X)
                {
                    if (p.Y >= obs[q].Y && p.Y <= obs[q].Y + obs[q].H)
                    {
                        return true;
                    }
                }else if (p.X >= obs[q].X && p.X + dx <= obs[q].X)
                {
                    if (p.Y >= obs[q].Y && p.Y <= obs[q].Y + obs[q].H)
                    {
                        return true;
                    }
                }
            }

            //범위
            if (p.X + dx < 0) return true;
            if (p.X + dx > width) return true;
            if (p.Y + dy < 0) return true;
            if (p.Y + dy > height) return true;

            return false;
        }
    }
}
