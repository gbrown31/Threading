using System.Collections.Concurrent;

namespace Threads.Test
{
    public class TrafficLightsTest
    {
        [Fact]
        public async Task CallThreadsToCrossCarsAsync()
        {
            ConcurrentBag<string> messages = new ConcurrentBag<string>();

            TrafficLightConcurrentDictionary trafficLight = new TrafficLightConcurrentDictionary();
            ConcurrentQueue<int> carsCrossedInOrder = new ConcurrentQueue<int>();

            List<Task> tasks = new List<Task>()
            {
                Task.Run(() => trafficLight.CarArrived(1, 1, 1, () => messages.Add("Turn green on road 1"), () => { messages.Add("Car 1 has crossed"); carsCrossedInOrder.Enqueue(1); })),
                Task.Run(() => trafficLight.CarArrived(2, 2, 3, () => messages.Add("Turn green on road 2"), () => {messages.Add("Car 2 has crossed"); carsCrossedInOrder.Enqueue(2); })),
                Task.Run(() => trafficLight.CarArrived(3, 1, 1,() => messages.Add("Turn green on road 1"),() => {messages.Add("Car 3 has crossed"); carsCrossedInOrder.Enqueue(3); }))
            };

            await Task.WhenAll(tasks);

            Assert.True(carsCrossedInOrder.Count == 3);
        }
        [Fact]
        public async Task CallThreadsToCrossCarsResetEventAsync()
        {
            ConcurrentBag<string> messages = new ConcurrentBag<string>();

            TrafficLightWitLock trafficLight = new TrafficLightWitLock();
            ConcurrentQueue<int> carsCrossedInOrder = new ConcurrentQueue<int>();

            List<Task> tasks = new List<Task>()
            {
                Task.Run(() => trafficLight.CarArrived(1, 1, 1, () => messages.Add("Turn green on road 1"), () => { messages.Add("Car 1 has crossed"); carsCrossedInOrder.Enqueue(1); })),
                Task.Run(() => trafficLight.CarArrived(2, 2, 3, () => messages.Add("Turn green on road 2"), () => {messages.Add("Car 2 has crossed"); carsCrossedInOrder.Enqueue(2); })),
                Task.Run(() => trafficLight.CarArrived(3, 1, 1,() => messages.Add("Turn green on road 1"),() => {messages.Add("Car 3 has crossed"); carsCrossedInOrder.Enqueue(3); }))
            };

            await Task.WhenAll(tasks);

            Assert.True(carsCrossedInOrder.Count == 3);
        }
    }
    public class TrafficLightWitLock
    {
        // lock for switching lights
        private object trafficLightLock = new object();
        // shared state between methods
        private int roadAtGreen = 1;

        public TrafficLightWitLock()
        {
        }

        public void CarArrived(
            int carId,         // ID of the car
            int roadId,        // ID of the road the car travels on. Can be 1 (road A) or 2 (road B)
            int direction,     // Direction of the car
            Action turnGreen,  // Use turnGreen() to turn light to green on current road
            Action crossCar    // Use crossCar() to make car cross the intersection
        )
        {
            lock (trafficLightLock)
            {
                if (roadAtGreen != roadId)
                {
                    turnGreen();
                    roadAtGreen = roadId;
                }
                crossCar();
            }
        }
    }

    public class TrafficLightConcurrentDictionary
    {
        // lock for switching lights
        private object trafficLightLock = new object();
        // shared state between methods
        int roadAtGreen = 1;
        private ConcurrentDictionary<int, ConcurrentQueue<(int, Action)>> roadsAndCars = new();

        public TrafficLightConcurrentDictionary()
        {
            roadsAndCars.TryAdd(1, new ConcurrentQueue<(int, Action)>());
            roadsAndCars.TryAdd(2, new ConcurrentQueue<(int, Action)>());
        }

        public void CarArrived(
            int carId,         // ID of the car
            int roadId,        // ID of the road the car travels on. Can be 1 (road A) or 2 (road B)
            int direction,     // Direction of the car
            Action turnGreen,  // Use turnGreen() to turn light to green on current road
            Action crossCar    // Use crossCar() to make car cross the intersection
        )
        {
            // add car to queue on appropriate road
            if (roadsAndCars.TryGetValue(roadId, out var cars))
            {
                cars.Enqueue((carId, crossCar));
            }

            if (roadAtGreen != roadId)
            {
                // wait for queue of cars on other road to clear
                bool carsWaitingOnOtherRoad = true;
                while (carsWaitingOnOtherRoad)
                {
                    carsWaitingOnOtherRoad = roadsAndCars[roadAtGreen].Count > 0;
                }

                turnGreen();
                lock (trafficLightLock)
                {
                    roadAtGreen = roadId;
                }
            }

            var carQueue = roadsAndCars[roadId];
            while (carQueue.Count > 0)
            {
                if (carQueue.TryDequeue(out var car))
                {
                    car.Item2();
                }
            }
        }
    }
}
