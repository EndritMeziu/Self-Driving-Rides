using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;

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
        public static double Beta = 40;
        public static List<Car> bestSolution = new List<Car>();
        public static string savePath = string.Empty;
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("You should specify the path of the file.");
                return;
            }
            string filePath = args[0];
            savePath = args[1];
            readRides = ReadFile(filePath);
            Console.WriteLine("Application in running...");
            SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);
            for (int i = 0; i < NumCars; i++)
            {
                Car car = new Car();
                car.setCarId(i + 1);
                readCars.Add(car);
            }

            Car toFilterCar = readCars.ElementAt(0);
            readRides = readRides
                .Where(x => (x.getRideDistance() +
                Math.Max(toFilterCar.distanceToRide(x.getStartX(), x.getStartY()), x.earliestStart)) <= x.latestFinish).ToList();
            int popSize = 0;
            Dictionary<int, List<Car>> population = new Dictionary<int, List<Car>>();
            //Population Generation

            List<Car> firstSol = new List<Car>();
            firstSol = ReinitializeCars(readCars);
            List<Ride> firsSolRides = CopyRides(readRides);
            while(true)
            {
                firsSolRides = firsSolRides.OrderBy(x => x.earliestStart).ThenBy(x => x.latestFinish).ToList();
                firstSol = firstSol.OrderBy(x => x.rides.Count()).ToList();
                Car c = firstSol[0];
                if (c.rides.Count > 0)
                    break;
                int rideKey = firsSolRides.ElementAt(0).getRideId();
                Ride r = firsSolRides.Where(x => x.getRideId() == rideKey).FirstOrDefault();

                int distanceToRide = c.distanceToRide(r.getStartX(), r.getStartY()) + c.getCurrentCarDistance();

                if (distanceToRide < r.earliestStart)
                    distanceToRide = r.earliestStart;

                int distance = distanceToRide + r.getRideDistance();
                if (distance < Steps && distance < r.latestFinish)
                {
                    c.addRide(r.getRideId());
                    firsSolRides.Remove(r);
                    c.setPositionX(r.getEndX());
                    c.setPositionY(r.getEndY());
                    c.setCurrentCarDistance(distance);
                    c.addRideCost(distance);
                }

            }

            bestSolution = generateInitialSolution(getRemainingRides(firstSol), firstSol);

            while (popSize < 1)
            {
                int counter = 0;
                List<Ride> rides = ReinitializeRides(readRides);
                List<Car> cars = new List<Car>();
                cars = ReinitializeCars(readCars);
                while (true)
                {

                    /* Representation
                     * List of Cars
                     * Each List element is a car and has a list of rides
                     * */

                    rides = rides.OrderBy(x => x.latestFinish).ToList();

                    cars = cars.OrderBy(x => x.getRides().Count).ToList();
                    
                    Random random = new Random();
                    int ridesSize = rides.Count();
                    Car c = cars.ElementAt(0);

                    Dictionary<int, double> evaluatedRides = RideUsefulness(rides, c);
                    //evaluatedRides = evaluatedRides.Take(random.Next() % evaluatedRides.Count()).OrderBy(x => Guid.NewGuid()).ToDictionary(x => x.Key,y=>y.Value);
                    //Car c = cars.Where(x => x.getCurrentCarDistance() +
                    //            r.getRideDistance() +
                    //            x.distanceToRide(r.getStartX(), r.getStartY()) < Steps
                    //            &&
                    //            x.getCurrentCarDistance() +
                    //            r.getRideDistance() +
                    //            x.distanceToRide(r.getStartX(), r.getStartY()) < r.latestFinish).FirstOrDefault();
                    

                    int rideKey = evaluatedRides.ElementAt(0).Key;
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

                        rideKey = evaluatedRides.ElementAt(0).Key;

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
                List<Car> nextSol = generateInitialSolution(getRemainingRides(cars), cars);
                if (calculateFitnessScore(nextSol) > calculateFitnessScore(bestSolution))
                    bestSolution = CopySolution(nextSol);
            }

            
            double bestSolutionFitness = calculateFitnessScore(bestSolution.ToList());
            Console.WriteLine("Initial Solution is generated...");

            printSolution(bestSolution, savePath);
            

            int[] timeDistribution = new int[30];
            Random randomD = new Random();
            
            for (int i = 0; i < timeDistribution.Length; i++)
                timeDistribution[i] = randomD.Next() % 4;

            int[] ridePenalties = new int[NumRides];
            for (int i = 0; i < ridePenalties.Length; i++) 
                ridePenalties[i] = 0;

            foreach (var ride in readRides)
                ride.canBeExplored = 1;

            Console.WriteLine("GLS Algorithm....");
            GLS(readRides, 1000, timeDistribution, ridePenalties, bestSolution,savePath);
            
            //#endregion
            
        }


        static void updateCarRides(Car c)
        {
            c.setCurrentCarDistance(0);
            c.setPositionX(0);
            c.setPositionY(0);
            c.ridesCost = new List<int>();
            foreach(var ride in c.rides)
            {
                Ride r = readRides.Find(x => x.getRideId() == ride);
                int distanceToRide = c.distanceToRide(r.getStartX(), r.getStartY()) + c.getCurrentCarDistance();

                if (distanceToRide < r.earliestStart)
                    distanceToRide = r.earliestStart;

                int distance = distanceToRide + r.getRideDistance();
                if (distance < Steps && distance < r.latestFinish)
                {
                    c.setPositionX(r.getEndX());
                    c.setPositionY(r.getEndY());
                    c.setCurrentCarDistance(distance);
                    c.addRideCost(distance);
                }
            }
        }

        static List<Car> SwapRidesBetweenCars(List<Car> bestSolution,int car1,int car2,int ride1,int ride2,string savePath)
        {
            List<Car> newSol = CopySolution(bestSolution);
            Car c1 = newSol[car1];
            Car c2 = newSol[car2];
            Ride r1 = readRides.Find(x => x.getRideId() == c1.rides[ride1]);
            Ride r2 = readRides.Find(x => x.getRideId() == c2.rides[ride2]);

            
            int StepSizeCar2 = 0;
            Car newCar2 = new Car();
            int car2Steps = 0;
            if (c1.rides.Count > 2 && c2.rides.Count > 2)
            {
                if (ride2 == c2.ridesCost.Count() - 1)
                {
                    StepSizeCar2 = Steps - c2.ridesCost[ride2 - 1];
                    Ride prevRide = readRides.Find(x => x.getRideId() == c2.rides[ride2 - 1]);
                    newCar2.setPositionX(prevRide.getEndX());
                    newCar2.setPositionY(prevRide.getEndY());
                    car2Steps = c2.ridesCost[ride2 - 1];
                }
                else if (ride2 == 0)
                {
                    Ride nextRide = readRides.Find(x => x.getRideId() == c2.rides[ride2 + 1]);

                    newCar2.setPositionX(r2.getEndX());
                    newCar2.setPositionY(r2.getEndY());
                    StepSizeCar2 = c2.ridesCost[ride2 + 1]
                        - newCar2.distanceToRide(nextRide.getStartX(), nextRide.getStartY())
                        - nextRide.getRideDistance();
                    newCar2.setPositionX(0);
                    newCar2.setPositionY(0);
                    car2Steps = 0;
                }
                else
                {
                    Ride nextRide = readRides.Find(x => x.getRideId() == c2.rides[ride2 + 1]);

                    newCar2.setPositionX(r2.getEndX());
                    newCar2.setPositionY(r2.getEndY());
                    StepSizeCar2 = c2.ridesCost[ride2 + 1]
                        - newCar2.distanceToRide(nextRide.getStartX(), nextRide.getStartY())
                        - nextRide.getRideDistance()
                        - c2.ridesCost[ride2 - 1];

                    Ride prevRide = readRides.Find(x => x.getRideId() == c2.rides[ride2 - 1]);
                    newCar2.setPositionX(prevRide.getEndX());
                    newCar2.setPositionY(prevRide.getEndY());
                    car2Steps = c2.ridesCost[ride2 - 1];
                }

                int StepSizeCar1 = 0;
                int car1Steps = 0;
                Car newCar1 = new Car();
                if (ride1 == c1.ridesCost.Count() - 1)
                {
                    StepSizeCar1 = Steps - c1.ridesCost[ride1 - 1];
                    car1Steps = c1.ridesCost[ride1 - 1];
                    Ride prevRide = readRides.Find(x => x.getRideId() == c1.rides[ride1 - 1]);
                    newCar1.setPositionX(prevRide.getEndX());
                    newCar1.setPositionY(prevRide.getEndY());
                }
                else if (ride2 == 0)
                {
                    Ride nextRide = readRides.Find(x => x.getRideId() == c1.rides[ride1 + 1]);

                    newCar1.setPositionX(r1.getEndX());
                    newCar1.setPositionY(r1.getEndY());
                    StepSizeCar1 = c1.ridesCost[ride1 + 1]
                        - newCar1.distanceToRide(nextRide.getStartX(), nextRide.getStartY())
                        - nextRide.getRideDistance();

                    newCar1.setPositionX(0);
                    newCar1.setPositionY(0);
                    car1Steps = 0;
                }
                else
                {
                    Ride nextRide = readRides.Find(x => x.getRideId() == c1.rides[ride1 + 1]);

                    newCar1.setPositionX(r1.getEndX());
                    newCar1.setPositionY(r1.getEndY());
                    StepSizeCar1 = c1.ridesCost[ride1 + 1]
                        - newCar1.distanceToRide(nextRide.getStartX(), nextRide.getStartY())
                        - nextRide.getRideDistance()
                        - c1.ridesCost[ride1 - 1];

                    Ride prevRide = readRides.Find(x => x.getRideId() == c1.rides[ride1 - 1]);
                    newCar1.setPositionX(prevRide.getEndX());
                    newCar1.setPositionY(prevRide.getEndY());
                    car1Steps = c1.ridesCost[ride1 - 1];
                }


                c1.removeRide(c1.rides[ride1]);
                c2.removeRide(c2.rides[ride2]);

                updateCarRides(c1);
                updateCarRides(c2);

                int swappedCar2Dist = newCar2.distanceToRide(r1.getStartX(), r1.getStartY());
                if (swappedCar2Dist + car2Steps < r1.earliestStart)
                    swappedCar2Dist = r1.earliestStart - car2Steps;

                swappedCar2Dist += r1.getRideDistance();

                int swappedCar1Dist = newCar1.distanceToRide(r2.getStartX(), r2.getStartY());
                if (swappedCar1Dist+car1Steps < r2.earliestStart)
                    swappedCar1Dist = r2.earliestStart-car1Steps;

                swappedCar1Dist += r2.getRideDistance();

                if (swappedCar1Dist <= StepSizeCar1 && swappedCar2Dist <= StepSizeCar2)
                {
                    c1.rides.Insert(ride1, r2.getRideId());
                    c2.rides.Insert(ride2, r1.getRideId());
                    updateCarRides(c1);
                    updateCarRides(c2);
                    bestSolution = CopySolution(newSol);
                    printSolution(bestSolution, savePath);
                }
                //ride1 to insert in car2
            }
            return bestSolution;

        }

        static List<Car> improveSol(List<Ride> remRides,List<Car> cars,int rideId)
        {
            remRides = remRides.Where(x => x.getRideId() != rideId).OrderBy(x => x.earliestStart).ToList();
            foreach (var ride in remRides)
            {
                List<RideQuality> rideQualities = new List<RideQuality>();
                foreach (Car c in cars)
                {
                    rideQualities.Add(c.quality(ride));
                }
                List<RideQuality> filteredList = rideQualities.Where(x => (x.finishStep <= ride.latestFinish)).OrderBy(x => x.pickUpStep).ToList();
                if (filteredList.Any())
                {
                    if (filteredList.Where(x => x.pickUpStep <= ride.earliestStart).Any())
                    {
                        var carId = filteredList.Where(x => x.pickUpStep <= ride.earliestStart).First().carId;
                        Car c = cars.Find(x => x.getCarId() == carId);
                        int distanceToRide = c.distanceToRide(ride.getStartX(), ride.getStartY()) + c.getCurrentCarDistance();

                        if (distanceToRide < ride.earliestStart)
                            distanceToRide = ride.earliestStart;

                        int distance = distanceToRide + ride.getRideDistance();
                        if (distance <= Steps && distance <= ride.latestFinish)
                        {
                            c.addRide(ride.getRideId());
                            //rides.Remove(ride);
                            c.setPositionX(ride.getEndX());
                            c.setPositionY(ride.getEndY());
                            c.setCurrentCarDistance(distance);
                            c.addRideCost(distance);
                        }

                    }
                    else
                    {
                        var carId = filteredList.First().carId;
                        Car c = cars.Find(x => x.getCarId() == carId);
                        int distanceToRide = c.distanceToRide(ride.getStartX(), ride.getStartY()) + c.getCurrentCarDistance();

                        if (distanceToRide < ride.earliestStart)
                            distanceToRide = ride.earliestStart;

                        int distance = distanceToRide + ride.getRideDistance();
                        if (distance <= Steps && distance <= ride.latestFinish)
                        {
                            c.addRide(ride.getRideId());
                            //rides.Remove(ride);
                            c.setPositionX(ride.getEndX());
                            c.setPositionY(ride.getEndY());
                            c.setCurrentCarDistance(distance);
                            c.addRideCost(distance);
                        }
                    }
                }
            }
            return cars;
        }


        static List<Car> improveInitialSolution(List<Ride> readRides, List<Car> cars)
        {
            List<Ride> rides = CopyRides(readRides);
            rides = rides.OrderBy(x => x.earliestStart).ToList();
            foreach (Ride ride in rides)
            {
                List<RideQuality> rideQualities = new List<RideQuality>();
                foreach (Car c in cars)
                {
                    rideQualities.Add(c.quality(ride));
                }
                List<RideQuality> filteredList = rideQualities.Where(x => (x.finishStep <= ride.latestFinish)).OrderBy(x => x.noSteps).ToList();
                if (filteredList.Any())
                {
                    if (filteredList.Where(x => x.pickUpStep <= ride.earliestStart).Any())
                    {
                        var carId = filteredList.Where(x => x.pickUpStep <= ride.earliestStart).First().carId;
                        Car c = cars.Find(x => x.getCarId() == carId);
                        int distanceToRide = c.distanceToRide(ride.getStartX(), ride.getStartY()) + c.getCurrentCarDistance();

                        if (distanceToRide < ride.earliestStart)
                            distanceToRide = ride.earliestStart;

                        int distance = distanceToRide + ride.getRideDistance();
                        if (distance <= Steps && distance <= ride.latestFinish)
                        {
                            c.addRide(ride.getRideId());
                            //rides.Remove(ride);
                            c.setPositionX(ride.getEndX());
                            c.setPositionY(ride.getEndY());
                            c.setCurrentCarDistance(distance);
                            c.addRideCost(distance);
                        }

                    }
                    else
                    {
                        var carId = filteredList.First().carId;
                        Car c = cars.Find(x => x.getCarId() == carId);
                        //Car c = cars.OrderBy(x => x.rides.Count()).ElementAt(0);
                        int distanceToRide = c.distanceToRide(ride.getStartX(), ride.getStartY()) + c.getCurrentCarDistance();

                        if (distanceToRide < ride.earliestStart)
                            distanceToRide = ride.earliestStart;

                        int distance = distanceToRide + ride.getRideDistance();
                        if (distance <= Steps && distance <= ride.latestFinish)
                        {
                            c.addRide(ride.getRideId());
                            //rides.Remove(ride);
                            c.setPositionX(ride.getEndX());
                            c.setPositionY(ride.getEndY());
                            c.setCurrentCarDistance(distance);
                            c.addRideCost(distance);
                        }
                    }
                }
            }

            
            return cars;
        }

        static List<Car> generateInitialSolution(List<Ride> readRides,List<Car> cars)
        {
            List<Ride> rides = CopyRides(readRides);

            rides = rides.OrderBy(x => x.earliestStart).ThenBy(x => x.latestFinish).ToList();
            foreach(Ride ride in rides)
            {
                List<RideQuality> rideQualities = new List<RideQuality>();
                foreach(Car c in cars)
                {
                    rideQualities.Add(c.quality(ride));
                }
                List<RideQuality> filteredList = rideQualities.Where(x => (x.finishStep <= ride.latestFinish)).OrderBy(x => x.noSteps).ToList();
                if(filteredList.Any())
                {
                    if(filteredList.Where(x => x.pickUpStep <= ride.earliestStart).Any())
                    {
                        
                        var carId = filteredList.Where(x => x.pickUpStep <= ride.earliestStart).First().carId;
                        Car c = cars.Find(x => x.getCarId() == carId);
                        int distanceToRide = c.distanceToRide(ride.getStartX(), ride.getStartY()) + c.getCurrentCarDistance();

                        if (distanceToRide < ride.earliestStart)
                            distanceToRide = ride.earliestStart;

                        int distance = distanceToRide + ride.getRideDistance();
                        if (distance <= Steps && distance <= ride.latestFinish)
                        {
                            c.addRide(ride.getRideId());
                            //rides.Remove(ride);
                            c.setPositionX(ride.getEndX());
                            c.setPositionY(ride.getEndY());
                            c.setCurrentCarDistance(distance);
                            c.addRideCost(distance);
                        }

                    }
                    else
                    {
                        var carId = filteredList.First().carId;
                        Car c = cars.Find(x => x.getCarId() == carId);
                        //Car c = cars.OrderBy(x => x.rides.Count()).ElementAt(0);
                        int distanceToRide = c.distanceToRide(ride.getStartX(), ride.getStartY()) + c.getCurrentCarDistance();

                        if (distanceToRide < ride.earliestStart)
                            distanceToRide = ride.earliestStart;

                        int distance = distanceToRide + ride.getRideDistance();
                        if (distance <= Steps && distance <= ride.latestFinish)
                        {
                            c.addRide(ride.getRideId());
                            //rides.Remove(ride);
                            c.setPositionX(ride.getEndX());
                            c.setPositionY(ride.getEndY());
                            c.setCurrentCarDistance(distance);
                            c.addRideCost(distance);
                        }
                    }
                }
            }
            return cars;
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

        
        static void GLS(List<Ride> rides, int totalTime, int[] timeDistribution, int[] penalties,List<Car> candidateSolution,string savePath)
        {
            List<Car> bestSolution = CopySolution(candidateSolution);
            List<Car> beginSolution = CopySolution(candidateSolution);
            double currentBestfitness = calculateFitnessScore(bestSolution.ToList());
            while(true)
            {
                Random r = new Random();
                int index = r.Next() % timeDistribution.Length;
                int count = 0;
                while (count < timeDistribution[index])
                {
                    count++;
                    List<Ride> rideList = rides.Where(x => x.canBeExplored == 1).ToList();
                    List<Car> cars = ReinitializeCars(readCars);
                    List<Car> newSol = Tweak(candidateSolution, count, penalties, rideList);
                    double fitness = calculateFitnessScore(newSol);
                    if (fitness > currentBestfitness)
                    {
                        currentBestfitness = fitness;
                        candidateSolution = CopySolution(newSol);
                        printSolution(newSol.ToList(), savePath);
                        bestSolution = CopySolution(newSol);
                    }



                    if (AdjustedQuality(newSol, penalties, Beta) > AdjustedQuality(candidateSolution, penalties, Beta))
                    {
                        candidateSolution = CopySolution(newSol);
                    }
                   
                }
                
                int[] penValues = Penalizability(candidateSolution, penalties);
                double maxVal = 0;
                int rideIndex = 0;
                for (int i = 0; i < penValues.Length; i++)
                {
                    if (penValues[i] > maxVal)
                    {
                        maxVal = penValues[i];
                        rideIndex = i;
                    }
                }

                
                penalties[rideIndex] += 3;
                for(int i=0;i<penalties.Length;i++)
                {
                    if (penalties[i] > 0 && i != rideIndex)
                        penalties[i] -= 1;
                }
                
                rides = updateActivateSet(rides,penalties);
                
            }
        }


        static void RemoveRidesFromCar(Car c, int rideId)
        {
            int rideIndex = -1;
            //find index of rideId in list of rides
            for(int i=0;i<c.rides.Count();i++)
            {
                if (c.rides[i] == rideId)
                    rideIndex = i;
            }
            if(rideIndex != -1)
            {
                //List to save rides which we will remove
                List<int> toRemove = new List<int>();
                for (int i = rideIndex; i < c.rides.Count(); i++)
                    toRemove.Add(c.rides[i]);

                foreach (int elem in toRemove)
                    c.rides.Remove(elem);

                c.ridesCost = new List<int>();

                c.setPositionX(0);
                c.setPositionY(0);
                c.setCurrentCarDistance(0);
                foreach (var ride in c.rides)
                {
                    Ride r = readRides.Find(x => x.getRideId() == ride);
                    int distance = c.distanceToRide(r.getStartX(), r.getStartY()) + c.getCurrentCarDistance();
                    if (distance < r.earliestStart)
                        distance = r.earliestStart;

                    distance += r.getRideDistance();

                    c.setCurrentCarDistance(distance);
                    c.ridesCost.Add(distance);
                    c.setPositionX(r.getEndX());
                    c.setPositionY(r.getEndY());
                    
                }    
            }

        }

        static Car swapWithOutside(Car c, int rideId,List<Ride> remRides,List<Car> candidateSolution)
        {
            List<int> carRides = c.rides.ToList();
            Ride ride = readRides.Find(x => x.getRideId() == rideId);
            List<int> rideCosts = c.ridesCost.ToList();
            int rideCostsIndex = -1;
            for (int i = 0; i < carRides.Count(); i++)
            {
                if (carRides[i] == rideId)
                    rideCostsIndex = i;
            }
            int fromDistance = -1;
            int toDistance = -1;
            int carPosX = 0;
            int carPosY = 0;
            if (rideCostsIndex == 0) {
                fromDistance = 0;
                toDistance = rideCosts[1];
            }
            else if(rideCostsIndex == rideCosts.Count() -1)
            {
                fromDistance = rideCosts[rideCostsIndex - 1];
                toDistance = Steps;
                carPosX = readRides.Find(x => x.getRideId() == carRides[rideCostsIndex - 1]).getEndX();
                carPosY = readRides.Find(x => x.getRideId() == carRides[rideCostsIndex - 1]).getEndY();
            }
            else
            {
                fromDistance = rideCosts[rideCostsIndex - 1];
                toDistance = rideCosts[rideCostsIndex + 1];
                carPosX = readRides.Find(x => x.getRideId() == carRides[rideCostsIndex - 1]).getEndX();
                carPosY = readRides.Find(x => x.getRideId() == carRides[rideCostsIndex - 1]).getEndY();
            }
            int availableDistance = toDistance - fromDistance;


            Ride newRide = remRides
                .Find(x =>
                 (Math.Max(x.distanceToRide(carPosX,carPosY,x.getStartX(),x.getStartY()), x.earliestStart)
                + x.getRideDistance()) <= availableDistance);
            if (newRide != null)
            {
                return swapRides(c, rideId, newRide.getRideId(), candidateSolution);
            }
            else
            {
                return null;
            }
        }
        
        static List<Ride> updateActivateSet(List<Ride> rides,int[] penalties)
        {
            for(int i=0;i<penalties.Length;i++)
            {
                if (penalties[i] > 0)
                    rides.Find(x => x.getRideId() == i).canBeExplored = 0;
            }
            return rides;
        }
        
        
        static int[] Penalizability(List<Car> carRides,int[] penalties)
        {
            int[] penValues = new int[NumRides];
            int[] indicators = getFeatureIndicator(carRides);
            foreach(Car car in carRides)
            {
                int count = 0;
                foreach(int rideId in car.getRides())
                {
                    Ride ride = readRides.Find(x => x.getRideId() == rideId);
                    if (count == 0)
                        penValues[ride.getRideId()] = indicators[ride.getRideId()] * car.getRidesCost().ElementAt(count) / (1 + penalties[ride.getRideId()]);
                    else
                    {
                        penValues[ride.getRideId()] = indicators[ride.getRideId()] * (car.getRidesCost().ElementAt(count) - car.getRidesCost().ElementAt(count-1)) / (1 + penalties[ride.getRideId()]);
                    }
                    count++;
                }
            }
            return penValues;
        }
        
        static int[] initializeIndicator()
        {
            int[] indicator = new int[NumRides];
            for(int i=0;i<indicator.Length;i++)
            {
                indicator[i] = 0;
            }
            return indicator;
        }
        
        static int[] getFeatureIndicator(List<Car> cars)
        {
            int[] indicator = initializeIndicator();
            foreach(Car c in cars)
            {
                foreach(int rideId in c.getRides())
                {
                    indicator[rideId] = 1;
                }
            }
            return indicator;
        }
        
        static double AdjustedQuality(List<Car> newSolution,int[] ridePenalties,double Beta)
        {
            int[] ridesIndicator = getFeatureIndicator(newSolution);
            double baseFitness = calculateFitnessScore(newSolution);
            double adjustedFitness = 0;
            for(int i=0;i<ridesIndicator.Length;i++)
            {
                adjustedFitness += (ridePenalties[i] * ridesIndicator[i]);
            }
            adjustedFitness *= (-Beta);
            adjustedFitness += baseFitness;
            return adjustedFitness;
        }

        static List<Car> CopySolution(List<Car> currentSol)
        {
            List<Car> returnedSol = new List<Car>();
            foreach(var car in currentSol)
            {
                Car c = new Car()
                {
                    rides = car.getRides().ToList(),
                    carScore = car.carScore,
                    ridesCost = car.getRidesCost().ToList()
                };
                c.setCarId(car.getCarId());
                c.setPositionX(car.getPositionX());
                c.setPositionY(car.getPositionY());
                c.setCurrentCarDistance(car.getCurrentCarDistance());
                returnedSol.Add(c);
            }
            return returnedSol;
        }

        static List<Ride> CopyRides(List<Ride> rides)
        {
            List<Ride> returedRides = new List<Ride>();
            foreach(var ride in rides)
            {
                Ride r = new Ride()
                {
                    earliestStart = ride.earliestStart,
                    latestFinish = ride.latestFinish
                };
                r.setRideId(ride.getRideId());
                r.setStartx(ride.getStartX());
                r.setStarty(ride.getStartY());
                r.setEndx(ride.getEndX());
                r.setEndy(ride.getEndY());
                returedRides.Add(ride);
            }
            return returedRides;
        }

        static int mostPenalizedCarRide(List<int> rides,int[] penalties)
        {
            int rideId = -1;
            int penValue = 0;
            rides = rides.OrderBy(x => Guid.NewGuid()).ToList();
            foreach(int ride in rides)
            {
                if(penalties[ride] > penValue)
                {
                    penValue = penalties[ride];
                    rideId = ride;
                }
            }
            return rideId;
        }


        static List<Car> Tweak2(List<Car> candidateSolution, int count, List<Ride> remainingRides)
        {
            foreach (Ride ride in remainingRides)
            {
                List<RideQuality> rideQualities = new List<RideQuality>();
                foreach (Car c in candidateSolution)
                {
                    rideQualities.Add(c.quality(ride));
                }
                List<RideQuality> filteredList = rideQualities.Where(x => (x.finishStep < ride.latestFinish)).OrderBy(x => x.noSteps).ToList();
                if (filteredList.Any())
                {
                    var filtered = filteredList.Where(x => x.pickUpStep <= ride.earliestStart).ToList();
                    if (filtered.Count > 0)
                    {
                        var carId = filtered.First().carId;
                        Car c = candidateSolution.Find(x => x.getCarId() == carId);
                        int distanceToRide = c.distanceToRide(ride.getStartX(), ride.getStartY()) + c.getCurrentCarDistance();

                        if (distanceToRide < ride.earliestStart)
                            distanceToRide = ride.earliestStart;

                        int distance = distanceToRide + ride.getRideDistance();
                        if (distance < Steps && distance < ride.latestFinish)
                        {
                            c.addRide(ride.getRideId());
                            //rides.Remove(ride);
                            c.setPositionX(ride.getEndX());
                            c.setPositionY(ride.getEndY());
                            c.setCurrentCarDistance(distance);
                            c.addRideCost(distance);
                            if (calculateFitnessScore(candidateSolution) > 176877)
                            {
                                Console.WriteLine("NewFIt:" + calculateFitnessScore(candidateSolution));
                            }
                        }
                    }
                                    }/*if (filteredList.Any())
                {
                    if (filteredList.Where(x => x.pickUpStep <= ride.earliestStart).Any())
                    {
                        var carId = filteredList.Where(x => x.pickUpStep <= ride.earliestStart).First().carId;
                        Car c = cars.Find(x => x.getCarId() == carId);
                        int distanceToRide = c.distanceToRide(ride.getStartX(), ride.getStartY()) + c.getCurrentCarDistance();

                        if (distanceToRide < ride.earliestStart)
                            distanceToRide = ride.earliestStart;

                        int distance = distanceToRide + ride.getRideDistance();
                        if (distance < Steps && distance < ride.latestFinish)
                        {
                            c.addRide(ride.getRideId());
                            //rides.Remove(ride);
                            c.setPositionX(ride.getEndX());
                            c.setPositionY(ride.getEndY());
                            c.setCurrentCarDistance(distance);
                            c.addRideCost(distance);
                        }

                    }
                    else
                    {
                        var carId = filteredList.First().carId;
                    }
                }*/
            }
            return candidateSolution;
        }
       static List<Car> Tweak(List<Car> candidateSolution,int count,int[] penalties,List<Ride> readRides)
       {

            List<Car> newSolution = new List<Car>();
            newSolution = CopySolution(candidateSolution);
            Random rand = new Random();
            int randCar = rand.Next() % NumCars;

            if (count % 8 == 0)
            {
                if (newSolution[randCar].rides.Count > 0)
                {
                    //List<int> badRides = GetWorstAddedRide(newSolution[randCar]);
                    //badRides = badRides.OrderBy(x => Guid.NewGuid()).ToList();
                    int rideId = mostPenalizedCarRide(newSolution[randCar].rides, penalties);
                    //int rideId = badRides.ElementAt(badRides.Count()-1);
                    if (rideId != -1)
                        RemoveRide(newSolution[randCar], readRides, rideId);
                }
                else
                    TryInsertRide(newSolution, newSolution[randCar], readRides, getRemainingRides(newSolution));
            }
            int tryInsert = 0;
            while (tryInsert < 10)
            {
                tryInsert++;
                rand = new Random();
                int carId = rand.Next() % readCars.Count() + 1; //1 based index
                TryInsertRide(newSolution, carId, readRides, getRemainingRides(newSolution));
            }
            int car = rand.Next() % NumCars;

            if (newSolution.ElementAt(car).rides.Count > 0 && getRemainingRides(newSolution).Count > 0)
            {
                int ride1 = rand.Next() % newSolution.ElementAt(car).rides.Count;
                int ride2 = rand.Next() % getRemainingRides(newSolution).Count;
                
                Car newCar = swapRides(newSolution.ElementAt(car), ride1, ride2,newSolution);
                if (newCar != null)
                {
                    if (validateSolution(newCar, newCar.getRides()))
                    {
                        newSolution.ElementAt(car).rides = newCar.getRides();
                        newSolution.ElementAt(car).ridesCost = newCar.ridesCost;
                        newSolution.ElementAt(car).setPositionX(newCar.getPositionX());
                        newSolution.ElementAt(car).setPositionY(newCar.getPositionY());
                        newSolution.ElementAt(car).setCurrentCarDistance(newCar.getCurrentCarDistance());
                    }
                }
                
            }

            return newSolution;
        }
        
        
        static List<Ride> getRemainingRides(List<Car> currentSolution)
        {
            List<Ride> remainingRides = readRides.ToList();
            foreach(var car in currentSolution)
            {
                foreach(var ride in car.rides)
                {
                    remainingRides.Remove(readRides.Find(x =>x.getRideId() == ride));
                }
            }
            return remainingRides;
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

       
        static void RemoveWorstAddedRide(Car c)
        {
            int rideId = -1;
            int rideCost = c.ridesCost[0];
            var ridesCosts = c.getRidesCost().ToList();
            for (int i = 1; i < ridesCosts.Count(); i++)
            {
                int nextCost = ridesCosts[i] - ridesCosts[i - 1];
                if (nextCost > rideCost)
                {
                    rideCost = nextCost;
                    rideId = c.rides.ElementAt(i);
                }
            }

            RemoveRide(c, readRides, rideId);
           
        }

        static int GetWorstAddedRide(Car c)
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
                }
            }
            return rideId;
        }

        static void RemoveRide(Car c, List<Ride> rides, int rideId)
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


        static List<Car> TryInsertRide(List<Car> cars, int carId, List<Ride> readRides, List<Ride> remainingRides)
        {
            List<Ride> rides = new List<Ride>();
            rides = CopyRides(readRides);
            Car c = cars.Find(x => x.getCarId() == carId);
            List<Ride> newRides = new List<Ride>();
            newRides = CopyRides(remainingRides);
            if (newRides.Count > 0)
            {
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
            return cars;
        }

        static void TryInsertRide(List<Car> cars, Car c, List<Ride> readRides,List<Ride> remainingRides)
        {
            List<Ride> rides = new List<Ride>();
            rides.AddRange(readRides);

            List<Ride> newRides = new List<Ride>();
            newRides.AddRange(remainingRides);
            Ride r = closestRide(c, newRides);
            int distanceToRide = c.distanceToRide(r.getStartX(), r.getStartY()) + c.getCurrentCarDistance();

            if (distanceToRide < r.earliestStart)
                distanceToRide = r.earliestStart;

            int distance = distanceToRide + r.getRideDistance();
            if (distance < Steps-1 && distance < r.latestFinish)
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
        static void printSolution(List<Car> bestSolution,string path)
        {
            
            StreamWriter file = new StreamWriter(path);
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

            //Console.WriteLine("Fitness:" + solutionFitness);
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
        static Car swapRides(Car c, int rideId1, int rideId2,List<Car> currentSolution)
        {
            Car newCar = new Car();
            newCar.rides = new List<int>();
            foreach (var ride in c.getRides())
            {
                newCar.rides.Add(ride);
            }
            int rideId1Index = -1;
            for(int i=0;i<c.rides.Count();i++)
            {
                if (c.rides[i] == rideId1)
                    rideId1Index = i;
            }
            if (rideId1Index != -1)
            {
                newCar.rides.RemoveAt(rideId1Index);
                newCar.rides.Insert(rideId1Index, rideId2);
            }
            newCar.setCurrentCarDistance(0);
            List<int> carRides = newCar.getRides().ToList();
            List<int> rideCosts = new List<int>();
            
            foreach(var cost in c.getRidesCost().ToList())
            {
                rideCosts.Add(cost);
            }
            
            for (int i = 0; i < carRides.Count; i++)
            {
                if (i == 0)
                {
                    newCar.setPositionX(0);
                    newCar.setPositionY(0);
                    Ride ride = readRides.Where(x => x.getRideId() == carRides[i]).FirstOrDefault();
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
                    Ride ride = readRides.Where(x => x.getRideId() == carRides[i]).FirstOrDefault();
                    int distance = newCar.distanceToRide(ride.getStartX(), ride.getStartY()) + newCar.getCurrentCarDistance();
                    if (distance < ride.earliestStart)
                        distance = ride.earliestStart;

                    distance += ride.getRideDistance();

                    if (distance > Steps && distance > ride.latestFinish)
                    {
                        return null;
                    }

                    newCar.setCurrentCarDistance(distance);
                    rideCosts[i] = distance;
                    newCar.setPositionX(ride.getEndX());
                    newCar.setPositionY(ride.getEndY());
                    
                }
            }

            if (newCar.getCurrentCarDistance() <= c.getCurrentCarDistance())
            {
                c.rides = newCar.rides;
                c.ridesCost = rideCosts;
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

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            printSolution(bestSolution, savePath);
            Environment.Exit(0);
            return true;
            
        }


    }

}