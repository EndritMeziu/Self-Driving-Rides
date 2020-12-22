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
        public static List<Car> readCars = new List<Car>();
        public static List<Ride> remainingRides = new List<Ride>();

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("You should specify the path of the file.");
                return;
            }
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
            //Population Generation
            
            
            
            while (popSize < 1)
            {
                int counter = 0;

                List<Ride> rides = ReinitializeRides(readRides);
                List<Car> cars = new List<Car>();
                cars = ReinitializeCars(readCars);
                Console.WriteLine("Adding child: " + popSize);
                while (true)
                {

                    /* Representation
                     * List of Cars
                     * Each List element is a car and has a list of rides
                     * */

                    rides = rides.OrderBy(x => x.latestFinish).ToList();

                    cars = cars.OrderBy(x => x.getRides().Count).ToList();
                    var filteredCars = cars.Take(cars.Count / 6).ToList();
                    Random random = new Random();
                    int ridesSize = rides.Count();
                    Car c = filteredCars.ElementAt(random.Next() % filteredCars.Count);

                    Dictionary<int, double> evaluatedRides = RideUsefulness(rides, c);
                    //evaluatedRides = evaluatedRides.Take(random.Next() % evaluatedRides.Count()).OrderBy(x => Guid.NewGuid()).ToDictionary(x => x.Key,y=>y.Value);
                    //Car c = cars.Where(x => x.getCurrentCarDistance() +
                    //            r.getRideDistance() +
                    //            x.distanceToRide(r.getStartX(), r.getStartY()) < Steps
                    //            &&
                    //            x.getCurrentCarDistance() +
                    //            r.getRideDistance() +
                    //            x.distanceToRide(r.getStartX(), r.getStartY()) < r.latestFinish).FirstOrDefault();
                    Dictionary<int, double> filteredRides = evaluatedRides
                                                           .Take(evaluatedRides.Count / 4)
                                                           .ToDictionary(x => x.Key, y => y.Value);

                    int rideKey = filteredRides.ElementAt(random.Next() % filteredRides.Count).Key;
                    Ride r = rides.Where(x => x.getRideId() == rideKey).FirstOrDefault();
 
                    int distanceToRide = c.distanceToRide(r.getStartX(), r.getStartY()) + c.getCurrentCarDistance();

                    if (distanceToRide < r.earliestStart)
                        distanceToRide = r.earliestStart;

                    int distance = distanceToRide + r.getRideDistance();
                    if (distance < Steps && distance < r.latestFinish)
                    {
                        c.addRide(r.getRideId());
                        rides.Remove(r);
                        c.setPositionX(r.getEndX());
                        c.setPositionY(r.getEndY());
                        c.setCurrentCarDistance(distance);
                        c.addRideCost(distance);
                        evaluatedRides.Remove(r.getRideId());
                    }
                    else
                    {

                        rideKey = evaluatedRides.ElementAt(random.Next() % evaluatedRides.Count()).Key;

                        r = rides.Where(x => x.getRideId() == rideKey).FirstOrDefault();

                        distanceToRide = c.distanceToRide(r.getStartX(), r.getStartY()) + c.getCurrentCarDistance();

                        if (distanceToRide < r.earliestStart)
                            distanceToRide = r.earliestStart;

                        distance = distanceToRide + r.getRideDistance();
                        if (distance < Steps && distance < r.latestFinish)
                        {
                            c.addRide(r.getRideId());
                            rides.Remove(r);
                            c.setPositionX(r.getEndX());
                            c.setPositionY(r.getEndY());
                            c.setCurrentCarDistance(distance);
                            c.addRideCost(distance);
                        }

                    }
                    counter++;

                    if (rides.Count == 0 || counter == 15000)
                        break;

                }
                population.Add(popSize, cars);
                popSize++;
            }

            List<Car> bestSolution = new List<Car>();
            double bestFitness = 0;

            foreach (var solution in population)
            {
                double solutionFitness = calculateFitnessScore(solution.Value.ToList());
                if (bestFitness < solutionFitness)
                {
                    bestFitness = solutionFitness;
                    bestSolution = solution.Value.ToList();
                }
            }

            double bestSolutionFitness = calculateFitnessScore(bestSolution.ToList());
            Console.WriteLine("Solution fitness: " + calculateFitnessScore(bestSolution.ToList()));

            printSolution(bestSolution, bestSolutionFitness);
            remainingRides = GetRemainingRides(bestSolution);


            //Hill Climbing with swap and insert rides
            //performHillClimbing(5000, bestSolution);

            /*Generating components for GRASP Method
            Dictionary<int, double> components = new Dictionary<int, double>();
            foreach (var ride in readRides)
            {
                components.Add(ride.getRideId(), GRASP_ride_fitness(ride));
            }

            //Console.WriteLine("GRASP Method");
            //GRASP(components, 100, 10, 10);*/

        }

        /// <summary>
        /// 
        /// </summary>
        static Dictionary<int,double> RideUsefulness(List<Ride> remainingRides,Car c)
        {
            Dictionary<int, double> keyValuePairs = new Dictionary<int, double>();
            
            foreach(var ride in remainingRides)
            {
                int distance = c.distanceToRide(ride.getStartX(), ride.getStartY());
                if (distance < ride.getEarliestStart())
                    distance = ride.getEarliestStart();
                keyValuePairs.Add(ride.getRideId(), distance);
            }
            keyValuePairs = keyValuePairs.OrderBy(x => x.Value).ToDictionary(x => x.Key, y => y.Value);
            return keyValuePairs;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="components">int,double --> rideId, fitness </param>
        /// <param name="totalTime"></param>
        /// <param name="p"></param>
        /// <param name="m"></param>
        static void GRASP(Dictionary<int, double> components, int totalTime, int p, int m)
        {
            int currentTime = 0;

            List<Car> bestSolution = ReinitializeCars(readCars);


            while (currentTime <= totalTime)
            {
                currentTime += 1;
                List<Car> currentSolution = ReinitializeCars(readCars);
                int count = 0;
                while (true)
                {
                    List<int> CPrimComponents = FeasibleComponents(components, currentSolution);
                    if (count > 2000)
                    {
                        if (calculateFitnessScore(currentSolution) > calculateFitnessScore(readCars))
                        {
                            bestSolution = currentSolution;
                            double fitness = calculateFitnessScore(currentSolution);
                            printSolution(currentSolution, fitness);
                            Console.WriteLine();
                        }
                        break;
                    }
                    else
                    {
                        List<int> CSecondComponents = new List<int>();
                        decimal percentAsNumber = p * (decimal.Parse(CPrimComponents.Count.ToString()) / 100);
                        CSecondComponents = CPrimComponents.Take((int)Math.Ceiling(percentAsNumber)).ToList();
                        Random r = new Random();
                        int ridePos = r.Next() % CSecondComponents.Count;
                        Car c = currentSolution.ElementAt(r.Next() % currentSolution.Count);
                        Ride ride = readRides.Find(x => x.getRideId() == CSecondComponents.ElementAt(ridePos));
                        int distanceToRide = c.distanceToRide(ride.getStartX(), ride.getStartY()) + c.getCurrentCarDistance();

                        if (distanceToRide < ride.earliestStart)
                            distanceToRide = ride.earliestStart;

                        int distance = distanceToRide + ride.getRideDistance();

                        if (distance < Steps && distance < ride.latestFinish)
                        {
                            c.addRide(ride.getRideId());
                            c.setPositionX(ride.getEndX());
                            c.setPositionY(ride.getEndY());
                            c.setCurrentCarDistance(distance);
                            c.addRideCost(distance);
                        }

                    }
                    count++;
                    if (count % 500 == 0)
                    {
                        double fitness = calculateFitnessScore(currentSolution);
                        Console.WriteLine("Fitness: " + fitness);
                    }

                }

            }
        }

        static List<int> FeasibleComponents(Dictionary<int, double> components, List<Car> cars)
        {
            List<int> CPrimComponents = new List<int>();
            List<Car> clonedCars = ReinitializeCars(cars);
            components = components.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            foreach (var comp in components)
            {
                bool hasRide = false;
                foreach (var car in cars)
                {
                    if (car.rides.Contains(comp.Key))
                    {
                        hasRide = true;
                        break;
                    }
                }
                if (hasRide)
                    continue;
                foreach (var car in cars)
                {
                    car.addRide(comp.Key);
                    if (validateSolution(car, car.getRides()))
                    {
                        CPrimComponents.Add(comp.Key);
                        car.removeRide(comp.Key);
                        break;
                    }
                    car.removeRide(comp.Key);
                }
                if (CPrimComponents.Count > 20)
                    break;
            }

            return CPrimComponents;
        }

        static double GRASP_ride_fitness(Ride r)
        {
            return Math.Sqrt(Math.Pow(r.getStartX(), 2) + Math.Pow(r.getStartY(), 2));
        }

        static void performHillClimbing(int numIterations, List<Car> bestSolution)
        {
            int count = 0;
            while (true)
            {
                count++;
                if (count % 500 == 0)
                {
                    Random rand = new Random();
                    int randCar = rand.Next() % NumCars;
                    List<int> badRides = GetWorstAddedRide(bestSolution[randCar]);
                    badRides = badRides.OrderBy(x => Guid.NewGuid()).ToList();
                    int rideId = badRides.ElementAt(rand.Next() % badRides.Count());
                    if (rideId != -1)
                        RemoveRide(bestSolution[randCar], readRides, rideId);
                }

                if (count % 50 == 0)
                {
                    Random rand = new Random();
                    int randCar = rand.Next() % NumCars;
                    TryInsertRide(bestSolution, bestSolution.ElementAt(randCar), readRides);
                }
                Random r = new Random();
                int car = r.Next() % NumCars;
                Thread.Sleep(10);
                if (bestSolution.ElementAt(car).rides.Count > 0)
                {
                    int ride1 = r.Next() % bestSolution.ElementAt(car).rides.Count;
                    Thread.Sleep(10);
                    int ride2 = r.Next() % bestSolution.ElementAt(car).rides.Count;
                    Car newCar = swapRides(readRides, bestSolution.ElementAt(car), ride1, ride2);
                    if (newCar != null)
                    {
                        bool result = validateSolution(newCar, newCar.getRides());
                        if (result == true)
                        {
                            TryInsertRide(bestSolution, newCar, readRides);
                            bestSolution.ElementAt(car).rides = newCar.getRides();
                            bestSolution.ElementAt(car).ridesCost = newCar.ridesCost;
                            bestSolution.ElementAt(car).setPositionX(newCar.getPositionX());
                            bestSolution.ElementAt(car).setPositionY(newCar.getPositionY());
                            bestSolution.ElementAt(car).setCurrentCarDistance(newCar.getCurrentCarDistance());

                        }
                    }
                }
                if (count % 100 == 0)
                {
                    double fitness = calculateFitnessScore(bestSolution.ToList());
                    Console.WriteLine(fitness);
                }

                if (count > numIterations)
                {
                    double fitness = calculateFitnessScore(bestSolution.ToList());
                    printSolution(bestSolution, fitness);
                    break;
                }


            }
        }

        static List<int> GetWorstAddedRide(Car c)
        {
            int rideId = -1;
            int rideCost = c.ridesCost[0];
            var ridesCosts = c.getRidesCost().ToList();
            List<int> badRides = new List<int>();
            badRides.Add(c.rides.ElementAt(0));
            for (int i = 1; i < ridesCosts.Count(); i++)
            {
                int nextCost = ridesCosts[i] - ridesCosts[i - 1];
                if (nextCost > rideCost)
                {
                    rideCost = nextCost;
                    rideId = c.rides.ElementAt(i);
                    badRides.Add(rideId);
                }
            }
            return badRides;
        }

        static void RemoveRide(Car c, List<Ride> readRides, int rideId)
        {
            c.rides.Remove(rideId);
            List<int> carRides = c.getRides().ToList();
            c.setCurrentCarDistance(0);
            c.ridesCost = new List<int>();
            for (int i = 0; i < c.rides.Count; i++)
            {
                if (i == 0)
                {
                    c.setPositionX(0);
                    c.setPositionY(0);
                    Ride ride = readRides.Where(x => x.getRideId() == carRides[i]).FirstOrDefault();
                    int distance = c.distanceToRide(ride.getStartX(), ride.getStartY()) + c.getCurrentCarDistance();
                    if (distance < ride.earliestStart)
                        distance = ride.earliestStart;

                    distance += ride.getRideDistance();

                    c.setCurrentCarDistance(distance);
                    c.ridesCost.Add(distance);
                    c.setPositionX(ride.getEndX());
                    c.setPositionY(ride.getEndY());
                }
                else
                {
                    Ride ride = readRides.Where(x => x.getRideId() == carRides[i]).FirstOrDefault();
                    int distance = c.distanceToRide(ride.getStartX(), ride.getStartY()) + c.getCurrentCarDistance();
                    if (distance < ride.earliestStart)
                        distance = ride.earliestStart;

                    distance += ride.getRideDistance();

                    c.setCurrentCarDistance(distance);
                    c.ridesCost.Add(distance);
                    c.setPositionX(ride.getEndX());
                    c.setPositionY(ride.getEndY());
                }
            }
        }


        static void TryInsertRide(List<Car> cars, Car c, List<Ride> readRides)
        {
            List<Ride> rides = readRides.ToList();

            List<Ride> newRides = remainingRides.ToList();
            Ride r = closestRide(c, newRides);
            int distanceToRide = c.distanceToRide(r.getStartX(), r.getStartY()) + c.getCurrentCarDistance();

            if (distanceToRide < r.earliestStart)
                distanceToRide = r.earliestStart;

            int distance = distanceToRide + r.getRideDistance();
            if (distance < Steps && distance < r.latestFinish)
            {
                c.addRide(r.getRideId());
                rides.Remove(r);
                remainingRides.Remove(r);
                c.setPositionX(r.getEndX());
                c.setPositionY(r.getEndY());
                c.setCurrentCarDistance(distance);
                c.addRideCost(distance);
            }
        }

        static List<Ride> GetRemainingRides(List<Car> cars)
        {
            List<Ride> allowedRides = readRides.ToList();
            List<Ride> newRides = new List<Ride>();
            foreach (var car in cars)
            {
                newRides = allowedRides.Where(x => !car.rides.Contains(x.getRideId())).ToList();
                allowedRides = newRides.ToList();
            }

            return newRides;

        }

        static KeyValuePair<int, List<Car>> RankSelect(Dictionary<int, List<Car>> rep)
        {
            int popSize = rep.Count;
            double F = (popSize * (popSize + 1)) / 2;

            Dictionary<int, double> repFitnessValues = new Dictionary<int, double>();

            foreach (var elem in rep)
            {
                double repFitness = calculateFitnessScore(elem.Value.ToList());
                repFitnessValues.Add(elem.Key, repFitness);
            }

            //Sorting the list based on the elements fitness
            repFitnessValues = repFitnessValues.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

            //Replacing fitness values with new values Rank_i/F
            foreach (var elem in repFitnessValues.ToDictionary(x => x.Key, x => x.Value))
            {
                repFitnessValues[elem.Key] = double.Parse(popSize.ToString()) / F;
                popSize--;
            }

            //Generating a random number less than one and selecting one individual
            Random rnd = new Random();
            double value = rnd.NextDouble() % 1;
            foreach (var elem in repFitnessValues)
            {
                if (elem.Value < value)
                {
                    var returnedRep = rep.FirstOrDefault(x => x.Key == elem.Key);
                    return returnedRep;
                }
            }
            return new KeyValuePair<int, List<Car>>();
        }

        static KeyValuePair<int, List<Car>> TournamentSelection(Dictionary<int, List<Car>> rep, int k)
        {
            int popSize = rep.Count;

            Dictionary<int, double> repFitnessValues = new Dictionary<int, double>();
            Dictionary<int, List<Car>> repValues = rep.ToDictionary(x => x.Key, x => x.Value);
            repValues = repValues.OrderBy(x => Guid.NewGuid()).ToDictionary(x => x.Key, x => x.Value);


            Dictionary<int, List<Car>> kElements = new Dictionary<int, List<Car>>();
            int i = 0;
            foreach (var elem in repValues)
            {
                kElements.Add(elem.Key, elem.Value);
                i++;
                if (i == k)
                    break;
            }

            double bestFitness = 0;
            KeyValuePair<int, List<Car>> bestSolution = new KeyValuePair<int, List<Car>>();
            foreach (var elem in kElements)
            {
                if (calculateFitnessScore(elem.Value) > bestFitness)
                {
                    bestFitness = calculateFitnessScore(elem.Value);
                    bestSolution = elem;
                }
            }
            return bestSolution;
        }
        static void printSolution(List<Car> bestSolution, double solutionFitness)
        {
            StreamWriter file = new StreamWriter(@"C:\Users\USER\Desktop\HashCode2018\Validator\c_no_hurry.out");
            string line;
            foreach (var car in bestSolution)
            {
                List<int> carRides = car.getRides().ToList();
                string carRidesStr = string.Empty;
                foreach (var ride in carRides)
                    carRidesStr += " " + ride;
                line = car.getRides().Count + carRidesStr;
                file.WriteLine(line);
            }

            Console.WriteLine("Fitness:" + solutionFitness);
            file.Close();
        }

        static double calculateFitnessScore(List<Car> mainCars)
        {
            double fitness = 0;
            List<Car> cars = mainCars.ToList();
            foreach (var car in cars)
            {
                car.setPositionX(0);
                car.setPositionY(0);
                car.setCurrentCarDistance(0);
                foreach (var ride in car.getRides())
                {
                    Ride r = readRides.FirstOrDefault(x => x.getRideId() == ride);
                    int distanceToRide = car.distanceToRide(r.getStartX(), r.getStartY()) + car.getCurrentCarDistance();
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
                    car.setCurrentCarDistance(distanceToRide + r.getRideDistance());
                }
            }

            return fitness;
        }

        static bool validateSolution(Car c, List<int> rides)
        {
            Car newCar = new Car();
            newCar.rides = c.getRides();
            int count = 0;

            if (rides.GroupBy(x => x).ToList().Count != rides.Count)
                return false;

            foreach (var ride in rides)
            {
                Ride r = readRides.Find(x => x.getRideId() == ride);
                int distanceToRide = newCar.distanceToRide(r.getStartX(), r.getStartY()) + newCar.getCurrentCarDistance();

                if (distanceToRide < r.earliestStart)
                    distanceToRide = r.earliestStart;

                int distance = distanceToRide + r.getRideDistance();
                if (distance < Steps && distance < r.latestFinish)
                {
                    count++;
                    newCar.setPositionX(r.getEndX());
                    newCar.setPositionY(r.getEndY());
                    newCar.setCurrentCarDistance(distance);
                    newCar.addRideCost(distance);
                }
            }

            if (count == rides.Count)
                return true;
            else
                return false;
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

        static Ride closestRide(Car newCar, List<Ride> rides)
        {
            int xPos = newCar.getPositionX();
            int yPos = newCar.getPositionY();
            int maxDist = int.MaxValue;
            Ride r = rides[0];
            foreach (var ride in rides)
            {
                if (newCar.distanceToRide(ride.getStartX(), ride.getStartY()) < maxDist)
                {
                    maxDist = newCar.distanceToRide(ride.getStartX(), ride.getStartY());
                    r = ride;
                }
            }

            return r;
        }
        static Car swapRides(List<Ride> rides, Car c, int rideId1, int rideId2)
        {
            List<int> rideCosts = c.getRidesCost().ToList();
            List<int> carRides = c.getRides().ToList();

            int temp = carRides[rideId1];
            carRides[rideId1] = carRides[rideId2];
            carRides[rideId2] = temp;
            Car newCar = new Car();
            newCar.setCurrentCarDistance(0);
            for (int i = 0; i < carRides.Count; i++)
            {
                if (i == 0)
                {
                    newCar.setPositionX(0);
                    newCar.setPositionY(0);
                    Ride ride = rides.Where(x => x.getRideId() == carRides[i]).FirstOrDefault();
                    int distance = newCar.distanceToRide(ride.getStartX(), ride.getStartY()) + newCar.getCurrentCarDistance();
                    if (distance < ride.earliestStart)
                        distance = ride.earliestStart;

                    distance += ride.getRideDistance();

                    newCar.setCurrentCarDistance(distance);
                    rideCosts[i] = distance;
                    newCar.setPositionX(ride.getEndX());
                    newCar.setPositionY(ride.getEndY());
                }
                else
                {
                    Ride ride = rides.Where(x => x.getRideId() == carRides[i]).FirstOrDefault();
                    int distance = newCar.distanceToRide(ride.getStartX(), ride.getStartY()) + newCar.getCurrentCarDistance();
                    if (distance < ride.earliestStart)
                        distance = ride.earliestStart;

                    distance += ride.getRideDistance();

                    newCar.setCurrentCarDistance(distance);
                    rideCosts[i] = distance;
                    newCar.setPositionX(ride.getEndX());
                    newCar.setPositionY(ride.getEndY());
                }
            }
            if (newCar.getCurrentCarDistance() < c.getCurrentCarDistance())
            {
                newCar.rides = carRides;
                newCar.ridesCost = rideCosts;
                return newCar;
            }
            return null;
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