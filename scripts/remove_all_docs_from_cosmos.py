from azure.cosmos import CosmosClient, PartitionKey, exceptions
from dotenv import load_dotenv
import os

# Load environment variables
load_dotenv()

# Initialize Cosmos Client
url = os.getenv("COSMOS_DB_URL")
key = os.getenv("COSMOS_DB_KEY")
client = CosmosClient(url, credential=key)

# Select database
database_name = os.getenv("DATABASE_NAME")
database = client.get_database_client(database_name)

# Select container
container_name = os.getenv("CONTAINER_NAME")
container = database.get_container_client(container_name)

# Query the documents in the container
for item in container.query_items(
        query='SELECT * FROM c',
        enable_cross_partition_query=True):

    # Delete each document
    container.delete_item(item, partition_key=item['id'])

print("All documents deleted.")