
import pika
import hashlib
import json

user = 'guest'
password = 'guest'
host = 'rabbitmq'
port = 5672
queue_name = 'crypto-puzzle-inquiries'
reply_queue_name = 'crypto-puzzle-responses'


def solve_crypto_puzzle(string, difficulty, id_process, quantity):
    sha256 = hashlib.sha256()
    needle = '0' * difficulty
    solution_candidate = None
    n = id_process
    while True:
        solution_candidate = string + str(n)
        result = sha256.hexdigest(solution_candidate)
        if result[:difficulty] == needle:
            return solution_candidate
        n += quantity


credentials = pika.PlainCredentials(user, password)
parameters = pika.ConnectionParameters(host=host, port=port, credentials=credentials)
connection = pika.BlockingConnection(parameters)
channel = connection.channel()
channel.queue_declare(queue=queue_name, auto_delete=True)


def callback(ch, method, properties, body):
    json_payload = json.loads(body)
    string = json_payload['string']
    difficulty = json_payload['difficulty']
    id_process = json_payload['id_process']
    quantity = json_payload['quantity']
    solution = solve_crypto_puzzle(string, difficulty, id_process, quantity)

    channel.basic_publish(
        exchange='',
        routing_key=properties.reply_to,
        properties=pika.BasicProperties(correlation_id=properties.correlation_id),
        body=solution
    )


channel.basic_consume(queue=queue_name, on_message_callback=callback, auto_ack=True)

try:
    channel.start_consuming()
except KeyboardInterrupt:
    channel.close()
    connection.close()