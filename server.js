{
  "script": "const express = require('express');\nconst bodyParser = require('body-parser');\nconst mysql = require('mysql');\n\nconst dbConfig = {\n    host: 'your_database_host',\n    user: 'your_database_user',\n    password: 'your_database_password',\n    database: 'your_database_name',\n    port: 3000\n};\n\nconst connection = mysql.createConnection(dbConfig);\n\nconst app = express();\napp.use(bodyParser.json());\n\napp.get('/orders', (req, res) => {\n    // Implementation for GET orders\n});\n\napp.post('/orders', (req, res) => {\n    // Implementation for POST orders\n});\n\napp.listen(3000, () => {\n    console.log('Server running on port 3000');\n});"
}