using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SelfDrivingRides
{
    class Program
    {
        public static int NumRows;
        public static int NumCols;
        public static int NumCars;
        public static int NumRides;
        public static int BonusValue;
        public static int Steps;
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("You should specify the path of the file.");
                return;
            }
            List<Ride> readRides = new List<Ride>();
            List<Car> readCars = new List<Car>();
            string filePath = args[0];
            readRides = ReadFile(filePath);
            for (int i = 0; i < NumCars; i++)
            {
                Car car = new Car();
                car.setCarId(i + 1);
                readCars.Add(car);
            }

            
            int popSize = 0;
            List<List<Car>> population = new List<List<Car>>();
            while (popSize <= 30)
            {
                int counter = 0;
                popSize++;
                List<Ride> rides = ReinitializeRides(readRides);
                List<Car> cars = new List<Car>();
                cars = ReinitializeCars(readCars);
                Console.WriteLine("Adding child: "+popSize);
                while (true)
                {
                    
                    /* Representation
                     * List of Cars
                     * Each List element is a car and has a list of rides
                     * */
                    cars = cars.OrderBy(x => x.getRides().Count).ToList();
                    Random random = new Random();
                    int ridesSize = rides.Count();

                    Ride r = rides.ElementAt(random.Next() % ridesSize);

                    
                    Car c = cars.ElementAt(0);
                    int distanceToRide = c.distanceToRide(r.getStartX(), r.getStartY());
        
                    if (distanceToRide < r.earliestStart)
                        distanceToRide = r.earliestStart;

                    int distance = distanceToRide + r.getRideDistance();
                    if (distance + c.getCurrentCarDistance() <= Steps && distance <= r.latestFinish)
                    {
                        c.addRide(r.getRideId());
                        rides.Remove(r);
                        c.setPositionX(r.getEndX());
                        c.setPositionY(r.getEndY());
                        c.setCurrentCarDistance(distance + c.getCurrentCarDistance());
                        c.addRideCost(distance);
                        if (distanceToRide == r.earliestStart) 
                        {
                            c.carScore += (r.getRideDistance() + BonusValue);
                        }
                        else 
                        {
                            c.carScore += r.getRideDistance();
                        }
                    }
                    counter++;

                    if (rides.Count == 0 || counter == 15000)
                        break;

                }
                population.Add(cars);
            }

            var oneRes = population.ElementAt(0);
            List<Car> bestSolution = new List<Car>();
            double solutionFitness = 0;
            foreach (var solution in population)
            {
                if(solutionFitness < calculateFitness(solution))
                {
                    solutionFitness = calculateFitness(solution);
                    bestSolution = solution;
                }
            }

            //Print Solution
            printSolution(bestSolution, solutionFitness);

        }

        static void printSolution(List<Car> bestSolution,double solutionFitness)
        {
            StreamWriter file = new StreamWriter(@"C:\Users\USER\Desktop\HashCode2018\Validator\c_no_hurry.out");
            string line;
            foreach (var car in bestSolution)
            {
                Console.WriteLine(car.getRides().Count);
                List<int> carRides = car.getRides().ToList();
                string carRidesStr = string.Empty;
                foreach (var ride in carRides)
                    carRidesStr += " " + ride;
                Console.WriteLine();
                line = car.getRides().Count + carRidesStr;
                file.WriteLine(line);
            }

            Console.WriteLine("Fitness:" + solutionFitness);
            file.Close();
        }

        static double calculateFitness(List<Car> cars)
        {
            double fitness = 0;
            foreach(var car in cars)
            {
                fitness += car.carScore;
            }
            return fitness;
        }


        static List<Ride> ReinitializeRides(List<Ride> currentRides)
        {
            List<Ride> rides = new List<Ride>();
            foreach (var ride in currentRides)
                rides.Add(ride);

            return rides;
        }

        static List<Car> ReinitializeCars(List<Car> currentCars)
        {
            List<Car> cars = new List<Car>();
            foreach (var car in currentCars)
            {
                Car c = new Car();
                c.setCarId(car.getCarId());
                cars.Add(c);
            }
            return cars;
        }

        static Car closestCarToRide(Ride r, List<Car> cars)
        {
            int distance = int.MaxValue;
            Car c = null;
            foreach(var car in cars)
            {
                if(car.distanceToRide(r.getStartX(),r.getStartY()) < int.MaxValue)
                {
                    distance = car.distanceToRide(r.getStartX(), r.getStartY());
                    c = car;
                }
            }

            return c;
        }

        static void swapRides(List<Ride> rides, Car c, int rideId1, int rideId2)
        {
            List<int> rideCosts = c.getRidesCost();
            List<int> carRides = c.getRides();
            int firstItemIndex = carRides.IndexOf(rideId1);
            int secondItemIndex = carRides.IndexOf(rideId2);
            carRides[firstItemIndex] = rideId2;
            carRides[secondItemIndex] = rideId1;
            c.setCurrentCarDistance(0);
            for (int i = firstItemIndex; i < carRides.Count; i++)
            {
                if (firstItemIndex == 0)
                {
                    c.setPositionX(0);
                    c.setPositionY(0);
                    Ride ride = rides.Where(x => x.getRideId() == carRides[i]).FirstOrDefault();
                    int distance = c.distanceToRide(ride.getStartX(), ride.getStartY()) + ride.getRideDistance();
                    c.setCurrentCarDistance(distance);
                    rideCosts[i] = distance;
                }
                else
                {
                    Ride ride = rides.Where(x => x.getRideId() == carRides[i]).FirstOrDefault();
                    int distance = c.distanceToRide(ride.getStartX(), ride.getStartY()) + ride.getRideDistance();
                    c.setCurrentCarDistance(c.getCurrentCarDistance() + distance);
                    rideCosts[i] = distance;
                }
            }
            if (rideCosts.Sum() < c.rides.Sum())
            {
                c.rides = carRides;
                c.ridesCost = rideCosts;
            }
        }

        static List<Ride> ReadFile(string filePath)
        {
            StreamReader file = new StreamReader(filePath);
            string line;
            int count = 0;
            string[] slicedData;
            List<Ride> rides = new List<Ride>();
            while ((line = file.ReadLine()) != null)
            {
                slicedData = line.Split(' ');
                if (count == 0)
                {
                    //First row data   
                    if (slicedData.Length < 5)
                    {
                        Console.WriteLine("Incorrect file data");
                        return null;
                    }
                    else
                    {
                        NumRows = Convert.ToInt32(slicedData[0]);
                        NumCols = Convert.ToInt32(slicedData[1]);
                        NumCars = Convert.ToInt32(slicedData[2]);
                        NumRides = Convert.ToInt32(slicedData[3]);
                        BonusValue = Convert.ToInt32(slicedData[4]);
                        Steps = Convert.ToInt32(slicedData[5]);
                    }

                }
                else
                {
                    Ride ride = new Ride();
                    int rideId = count - 1;
                    ride.setRideId(rideId);
                    ride.setStartx(int.Parse(slicedData[0]));
                    ride.setStarty(int.Parse(slicedData[1]));
                    ride.setEndx(int.Parse(slicedData[2]));
                    ride.setEndy(int.Parse(slicedData[3]));
                    ride.setEarliestStart(int.Parse(slicedData[4]));
                    ride.setLatestFinish(int.Parse(slicedData[5]));
                    rides.Add(ride);
                }
                count++;
            }
            return rides;
        }
    }
}
