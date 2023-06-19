using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Security.Cryptography;
using System.Text;
using System;
namespace Charp
{
    public class Program
    {
        static void Main()
        {
            string user = "guest";
            string password = "guest";
            string host = "rabbitmq";
            int port = 5671;
            string queueName = "crypto-puzzle-inquiries";

       
            string solveCryptoPuzzle(string str, int difficulty, int id_pocess, int quantity)
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    string needle = new string('0', difficulty);
                    string solutionCandidate = null;
                    int n = id_pocess;
                    while (true)
                    {
                        solutionCandidate = str + n.ToString();
                        byte[] solutionCandidateBytes = System.Text.Encoding.UTF8.GetBytes(solutionCandidate);
                        byte[] resultBytes = sha256.ComputeHash(solutionCandidateBytes);
                        string result = BitConverter.ToString(resultBytes).Replace("-", string.Empty);
                        if (result.Substring(0, difficulty) == needle)
                            return solutionCandidate;
                        n += quantity;
                    }
                }
            }

            var factory = new ConnectionFactory()
            {
                UserName = user,
                Password = password,
                HostName = host,
                Port = port
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "", type: ExchangeType.Direct);

                channel.QueueDeclare(queue: queueName, durable: false, exclusive: false,
                                     autoDelete: true, arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {

                    var body = ea.Body.ToArray();
                    string payload = System.Text.Encoding.UTF8.GetString(body);
                    dynamic jsonPayload = JsonConvert.DeserializeObject(payload);
                    string str = jsonPayload["string"];
                    int difficulty = int.Parse(jsonPayload["difficulty"].ToString());
                    int id_pocess = Convert.ToInt32(jsonPayload["id_pocess"].ToString());
                    int quantity = Convert.ToInt32(jsonPayload["quantity"].ToString());
                    string solution = solveCryptoPuzzle(str, difficulty, id_pocess, quantity);

                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = ea.BasicProperties.CorrelationId;

                    var responseBytes = Encoding.UTF8.GetBytes(solution);
                    channel.BasicPublish(exchange: "", routingKey: ea.BasicProperties.ReplyTo,
                        basicProperties: replyProps, body: responseBytes);
                };

                channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

                Console.WriteLine("Press [Enter] to exit.");
                Console.ReadLine();
            }
        }
    }
}
