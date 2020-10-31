using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

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
        public static List<Ride> readRides = new List<Ride>();

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("You should specify the path of the file.");
                return;
            }
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
            Dictionary<int, List<Car>> population = new Dictionary<int, List<Car>>();
            while (popSize <= 30)
            {
                int counter = 0;
                
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
                    Car c = cars.Where(x => x.getCurrentCarDistance() +
                                            r.getRideDistance() +
                                            x.distanceToRide(r.getStartX(), r.getStartY()) < Steps
                                            &&
                                            x.getCurrentCarDistance() +
                                            r.getRideDistance() +
                                            x.distanceToRide(r.getStartX(), r.getStartY()) < r.latestFinish).FirstOrDefault();

                    if (c != null)
                    {
                        int distanceToRide = c.distanceToRide(r.getStartX(), r.getStartY());

                        if (distanceToRide < r.earliestStart)
                            distanceToRide = r.earliestStart;

                        int distance = distanceToRide + r.getRideDistance();
                        if (distance + c.getCurrentCarDistance() < Steps && distance + c.getCurrentCarDistance() < r.latestFinish)
                        {
                            c.addRide(r.getRideId());
                            rides.Remove(r);
                            c.setPositionX(r.getEndX());
                            c.setPositionY(r.getEndY());
                            c.setCurrentCarDistance(distance + c.getCurrentCarDistance());
                            c.addRideCost(distance);
                        }
                    }
                    else
                    {
                        c = cars.ElementAt(0);

                        int distanceToRide = c.distanceToRide(r.getStartX(), r.getStartY());

                        if (distanceToRide < r.earliestStart)
                            distanceToRide = r.earliestStart;

                        int distance = distanceToRide + r.getRideDistance();
                        if (distance + c.getCurrentCarDistance() < Steps && distance + c.getCurrentCarDistance() < r.latestFinish)
                        {
                            c.addRide(r.getRideId());
                            rides.Remove(r);
                            c.setPositionX(r.getEndX());
                            c.setPositionY(r.getEndY());
                            c.setCurrentCarDistance(distance + c.getCurrentCarDistance());
                            c.addRideCost(distance);
                        }

                    }
                    counter++;

                    if (rides.Count == 0 || counter == 15000)
                        break;

                }
                population.Add(popSize,cars);
                popSize++;
            }

            List<Car> bestSolution = new List<Car>();
            double bestFitness = 0;

            foreach (var solution in population)
            {
                double solutionFitness = calculateFitnessScore(solution.Value.ToList());
                if(bestFitness < solutionFitness)
                {
                    bestFitness = solutionFitness;
                    bestSolution = solution.Value.ToList();
                }
            }

            //Print Solution
            //printSolution(bestSolution, bestFitness);
            Console.WriteLine("Solution fitness: " + calculateFitnessScore(bestSolution.ToList()));
            KeyValuePair<int,List<Car>> newElement =  RankSelect(population);
            KeyValuePair<int, List<Car>> selectedElement = TournamentSelection(population, 10);
        }

        static KeyValuePair<int,List<Car>> RankSelect(Dictionary<int,List<Car>> rep)
        {
            int popSize = rep.Count;
            double F = (popSize * (popSize + 1)) / 2;

            Dictionary<int, double> repFitnessValues = new Dictionary<int, double>();

            foreach(var elem in rep)
            {
               double repFitness =  calculateFitnessScore(elem.Value.ToList());
               repFitnessValues.Add(elem.Key, repFitness);
            }

            //Sorting the list based on the elements fitness
            repFitnessValues = repFitnessValues.OrderByDescending(x => x.Value).ToDictionary(x => x.Key,x=>x.Value);

            //Replacing fitness values with new values Rank_i/F
            foreach (var elem in repFitnessValues.ToDictionary(x => x.Key, x=>x.Value))
            {
                repFitnessValues[elem.Key] = double.Parse(popSize.ToString()) / F;
                popSize--;
            }
            
            //Generating a random number less than one and selecting one individual
            Random rnd = new Random();
            double value = rnd.NextDouble() % 1;
            foreach(var elem in repFitnessValues)
            {
                if (elem.Value < value) 
                {
                    var returnedRep = rep.FirstOrDefault(x => x.Key == elem.Key);
                    return returnedRep;
                }
            }
            return new KeyValuePair<int, List<Car>>();
        }

        static KeyValuePair<int,List<Car>> TournamentSelection(Dictionary<int, List<Car>> rep , int k)
        {
            int popSize = rep.Count;
            double F = (popSize * (popSize + 1)) / 2;

            Dictionary<int, double> repFitnessValues = new Dictionary<int, double>();
            Dictionary<int, List<Car>> repValues = rep.ToDictionary(x => x.Key, x => x.Value);
            repValues = repValues.OrderBy(x => Guid.NewGuid()).ToDictionary(x => x.Key, x=>x.Value);


            Dictionary<int, List<Car>> kElements = new Dictionary<int, List<Car>>();
            int i = 0;
            foreach(var elem in repValues)
            {
                kElements.Add(elem.Key,elem.Value);
                i++;
                if (i == k)
                    break;
            }

            double bestFitness = 0;
            KeyValuePair<int,List<Car>> bestSolution = new KeyValuePair<int,List<Car>>();
            foreach(var elem in kElements)
            {
                if(calculateFitnessScore(elem.Value) > bestFitness)
                {
                    bestFitness = calculateFitnessScore(elem.Value);
                    bestSolution = elem;
                }
            }
            return bestSolution;
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

        static double calculateFitnessScore(List<Car> cars)
        {
            double fitness = 0;
            foreach(var car in cars)
            {
                car.setPositionX(0);
                car.setPositionY(0);
                foreach(var ride in car.getRides())
                {
                    Ride r = readRides.FirstOrDefault(x => x.getRideId() == ride);
                    int distanceToRide = car.distanceToRide(r.getStartX(), r.getStartY());
                    if (distanceToRide < r.earliestStart)
                        distanceToRide = r.earliestStart;

                    if (distanceToRide == r.earliestStart)
                    {
                        fitness += (r.getRideDistance() + BonusValue);
                    }
                    else
                    {
                        fitness += r.getRideDistance();
                    }
                    car.setPositionX(r.getEndX());
                    car.setPositionY(r.getEndY());

                }
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
