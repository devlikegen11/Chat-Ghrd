#include <iostream>
#include <stdlib.h>
#include <unistd.h>
#include <string.h>
#include <arpa/inet.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <pthread.h>
#include <vector>
#include <mariadb/conncpp.hpp>
#include <nlohmann/json.hpp>

#define BUF_SIZE 100
#define MAX_CLNT 256

using json = nlohmann::json;

void *handle_clnt(void *arg);
void send_msg(const char *msg, int len, int sender_sock, std::string id);
void error_handling(const char *msg);

int clnt_cnt = 0;
int clnt_socks[MAX_CLNT];
int user_socks[MAX_CLNT];
int cs_socks[MAX_CLNT];
bool cs_available[MAX_CLNT]; // 각 상담원의 가용 여부
int user_to_cs[MAX_CLNT];    // 유저와 상담원의 매핑
int cs_to_user[MAX_CLNT];    // 상담원과 유저의 매핑

int user_infonum;
std::string user_id;
pthread_mutex_t mutx;

class DB
{
protected:
  sql::Connection *conn;

public:
  sql::PreparedStatement *prepareStatement(const std::string &query) // ? 쿼리문 삽입
  {
    sql::PreparedStatement *stmt(conn->prepareStatement(query));
    return stmt;
  }

  sql::Statement *createStatement() // 쿼리문 즉시 실행
  {
    sql::Statement *stm(conn->createStatement());
    return stm;
  }

  void connect()
  {
    try
    {
      sql::Driver *driver = sql::mariadb::get_driver_instance();
      sql::SQLString url("jdbc:mariadb://10.10.21.115:3306/QA");
      sql::Properties properties({{"user", "QA"}, {"password", "1"}});
      conn = driver->connect(url, properties);
    }
    catch (sql::SQLException &e)
    {
      std::cerr << "Error Connecting to MariaDB Platform: " << e.what() << std::endl;
    }
  }

  bool attemptLogin(const std::string &ID, const std::string &PW) // 유저 로그인 bool타입으로 검사
  {
    try
    {
      std::cout << "유저 접속어탬프" << std::endl;
      std::unique_ptr<sql::Statement> stmnt(conn->createStatement());
      sql::ResultSet *res = stmnt->executeQuery("SELECT * FROM USER");
      while (res->next())
      {
        if (res->getString(2) == ID && res->getString(3) == PW)
        {
          return true;
        }
      }
    }
    catch (sql::SQLException &e)
    {
      std::cerr << "Error during login attempt: " << e.what() << std::endl;
    }
    return false;
  }

  bool attemptCS(const std::string &ID, const std::string &PW) // 상담원 로그인 bool타입으로 검사
  {
    try
    {
      std::cout << "상담원 접속어탬프" << std::endl;
      std::unique_ptr<sql::Statement> stmnt(conn->createStatement());
      sql::ResultSet *res2 = stmnt->executeQuery("SELECT * FROM CS");
      while (res2->next())
      {
        if (res2->getString(2) == ID && res2->getString(3) == PW)
        {
          return true;
        }
      }
    }
    catch (sql::SQLException &e)
    {
      std::cerr << "Error during login attempt: " << e.what() << std::endl;
    }
    return false;
  }

  void inChatHistory(std::string id, std::string chat) // 채팅내역 DB저장
  {
    try
    {
      std::unique_ptr<sql::PreparedStatement> stmnt(conn->prepareStatement("INSERT INTO CHATDB (USER_ID, CHAT_RECORD, CHAT_TIMESTAMP) VALUES (?, ?, NOW())"));
      stmnt->setString(1, id);
      stmnt->setString(2, chat);
      stmnt->executeQuery();
    }
    catch (sql::SQLException &e)
    {
      std::cerr << "Error inserting new task: " << e.what() << std::endl;
    }
  }

  json userinfo(const std::string &userid) // 사용자 정보 JSON[JSON]형식으로 만들어서 리턴
  {
    json user_info;
    try
    {

      std::unique_ptr<sql::PreparedStatement> stmnt(conn->prepareStatement("SELECT * FROM USER WHERE USER_ID = ?;"));
      stmnt->setString(1, userid);
      std::unique_ptr<sql::ResultSet> res3(stmnt->executeQuery());

      while (res3->next())
      {
        user_info["USER_NO"] = res3->getInt("USER_NO");
        user_info["USER_ID"] = res3->getString("USER_ID");
        user_info["USER_PASSWORD"] = res3->getString("USER_PASSWORD");
        user_info["USER_NAME"] = res3->getString("USER_NAME");
        user_info["USER_PHONE"] = res3->getString("USER_PHONE");
        user_info["USER_STATE"] = res3->getString("USER_STATE");
      }
    }
    catch (sql::SQLException &e)
    {
      std::cerr << "Error fetching user info: " << e.what() << std::endl;
    }
    return user_info;
  }

  json usersearch(const std::string &userid) // 검색한 유저ID에 맞는 채팅내역 가져오는 JSON함수. 위와같이 JSON 리턴
  {
    json user_search;
    std::vector<json> chat_record;
    try
    {
      std::unique_ptr<sql::PreparedStatement> stmnt(conn->prepareStatement("SELECT USER_ID, CHAT_RECORD FROM CHATDB WHERE USER_ID = ?;"));
      stmnt->setString(1, userid);
      std::unique_ptr<sql::ResultSet> res4(stmnt->executeQuery());

      while (res4->next())
      {
        json chat_cord;
        chat_cord["USER_ID"] = res4->getString("USER_ID");
        chat_cord["CHAT_RECORD"] = res4->getString("CHAT_RECORD");
        // chat_cord["CHAT_TIMESTAMP"] = res4->getString("CHAT_TIMESTAMP");
        chat_record.push_back(chat_cord);
      }
      user_search["CHAT_GO"] = chat_record;
    }
    catch (sql::SQLException &e)
    {
      std::cerr << "Error fetching user info: " << e.what() << std::endl;
    }
    return user_search;
  }

  ~DB() { delete conn; }
};

void *handle_clnt(void *arg)
{
  int clnt_sock = *((int *)arg);
  int str_len = 0;
  char msg[BUF_SIZE];
  bool is_user = false;
  bool is_consultant = false;
  int mapped_cs_sock = -1;

  while ((str_len = read(clnt_sock, msg, sizeof(msg))) != 0)
  {
    json received_json = json::parse(std::string(msg, str_len));
    std::string client_type = received_json["type"]; // 접속 클라이언트가 유저인지 상담원인지 type의 value값으로 구분
    DB db;
    db.connect();

    if (client_type == "user")
    {
      user_id = received_json["id"];
      std::string pw = received_json["pw"];
      bool login_success = db.attemptLogin(user_id, pw);
      json response;
      response["login_success"] = login_success;
      std::string response_str = response.dump();
      write(clnt_sock, response_str.c_str(), response_str.length());

      if (login_success) // 유저 채팅 로직
      {
        is_user = true;

        pthread_mutex_lock(&mutx);
        for (int i = 0; i < MAX_CLNT; i++)
        {
          if (cs_available[i])
          {
            mapped_cs_sock = cs_socks[i];
            user_to_cs[clnt_sock] = cs_socks[i];
            cs_to_user[cs_socks[i]] = clnt_sock;
            cs_available[i] = false;
            break;
          }
        }
        pthread_mutex_unlock(&mutx);

        while ((str_len = read(clnt_sock, msg, sizeof(msg))) != 0)
        {
          msg[str_len] = '\0';
          std::cout << msg << std::endl;
          send_msg(msg, str_len, clnt_sock, user_id);
          // db.inChatHistory(id, std::string(msg));
        }
      }
    }
    else if (client_type == "consultant")
    {
      std::string id = received_json["id"];
      std::string pw = received_json["pw"];
      bool login_success = db.attemptCS(id, pw);
      json response;
      response["login_success"] = login_success;
      std::string response_str = response.dump();
      write(clnt_sock, response_str.c_str(), response_str.length());

      if (login_success) // 클라이언트 채팅 로직
      {
        is_consultant = true;

        pthread_mutex_lock(&mutx);
        for (int i = 0; i < MAX_CLNT; i++)
        {
          if (cs_socks[i] == 0)
          {
            cs_socks[i] = clnt_sock;
            cs_available[i] = true;
            break;
          }
        }
        pthread_mutex_unlock(&mutx);

        while (true)
        {
          str_len = read(clnt_sock, msg, sizeof(msg) - 1);
          if (str_len <= 0)
          {
            break; // 클라이언트 연결이 끊어지면 루프 종료
          }

          msg[str_len] = '\0';
          std::string received_msg(msg);

          if (received_msg == "CHAT")
          {
            std::cout << "상담원 채팅 진입" << std::endl;
            // 채팅 모드로 진입
            while (true)
            {
              memset(msg, 0, BUF_SIZE);
              str_len = read(clnt_sock, msg, sizeof(msg) - 1);
              std::cout << msg << std::endl;
              if (str_len <= 0)
              {
                break; // 클라이언트 연결이 끊어지면 채팅 모드 종료
              }

              msg[str_len] = '\0';
              received_msg = std::string(msg);

              if (received_msg == "DBCHECK")
              {
                std::cout << "디비체크" << std::endl;
                memset(msg, 0, BUF_SIZE);
                str_len = read(clnt_sock, msg, sizeof(msg) - 1);
                msg[strlen(msg)] = 0;
                received_msg = std::string(msg);
                json user_info = db.userinfo(received_msg);
                json chat_history = db.usersearch(received_msg);

                json combined;
                combined["user_info"] = user_info;
                combined["chat_history"] = chat_history;

                std::string response_str = combined.dump();
                write(clnt_sock, response_str.c_str(), response_str.length());
              }
              else
              {
                ////채팅 메시지를 전송
                send_msg(msg, str_len, clnt_sock, id);
              }
            }
          }
          else if (received_msg == "DBCHECK")
          {
            memset(msg, 0, BUF_SIZE);
            str_len = read(clnt_sock, msg, sizeof(msg) - 1);
            msg[strlen(msg)] = 0;
            received_msg = std::string(msg);
            json user_info = db.userinfo(received_msg);
            json chat_history = db.usersearch(received_msg);

            json combined;
            combined["user_info"] = user_info;
            combined["chat_history"] = chat_history;

            std::string response_str = combined.dump();
            write(clnt_sock, response_str.c_str(), response_str.length());
          }
        }
      }
    }
  }

  pthread_mutex_lock(&mutx);
  for (int i = 0; i < clnt_cnt; i++)
  {
    if (clnt_sock == clnt_socks[i])
    {
      while (i++ < clnt_cnt - 1)
      {
        clnt_socks[i] = clnt_socks[i + 1];
      }
      break;
    }
  }
  clnt_cnt--;

  if (is_user)
  {
    int cs_sock = user_to_cs[clnt_sock];
    for (int i = 0; i < MAX_CLNT; i++)
    {
      if (cs_socks[i] == cs_sock)
      {
        cs_available[i] = true;
        break;
      }
    }
    user_to_cs[clnt_sock] = 0;
    cs_to_user[cs_sock] = 0;
  }

  if (is_consultant)
  {
    for (int i = 0; i < MAX_CLNT; i++)
    {
      if (cs_socks[i] == clnt_sock)
      {
        cs_socks[i] = 0;
        cs_available[i] = false;
        if (cs_to_user[clnt_sock] != 0)
        {
          user_to_cs[cs_to_user[clnt_sock]] = 0;
          cs_to_user[clnt_sock] = 0;
        }
        break;
      }
    }
  }

  pthread_mutex_unlock(&mutx);
  close(clnt_sock);

  return NULL;
}

void send_msg(const char *msg, int len, int sender_sock, std::string id)
{
  DB db;
  db.connect();
  pthread_mutex_lock(&mutx);
  int recipient_sock = -1;

  if (user_to_cs[sender_sock] != 0)
  {
    recipient_sock = user_to_cs[sender_sock];
  }
  else if (cs_to_user[sender_sock] != 0)
  {
    recipient_sock = cs_to_user[sender_sock];
  }

  if (recipient_sock != -1)
  {
    write(recipient_sock, msg, len);
    db.inChatHistory(id, std::string(msg));
  }

  pthread_mutex_unlock(&mutx);
}

void error_handling(const char *msg)
{
  fputs(msg, stderr);
  fputc('\n', stderr);
  exit(1);
}

int main(int argc, char *argv[])
{
  int serv_sock, clnt_sock;
  struct sockaddr_in serv_adr, clnt_adr;
  socklen_t clnt_adr_sz;
  pthread_t t_id;

  if (argc != 2)
  {
    printf("Usage : %s <port>\n", argv[0]);
    exit(1);
  }

  pthread_mutex_init(&mutx, NULL);
  memset(user_to_cs, 0, sizeof(user_to_cs));
  memset(cs_to_user, 0, sizeof(cs_to_user));

  serv_sock = socket(PF_INET, SOCK_STREAM, 0);
  memset(&serv_adr, 0, sizeof(serv_adr));
  serv_adr.sin_family = AF_INET;
  serv_adr.sin_addr.s_addr = htonl(INADDR_ANY);
  serv_adr.sin_port = htons(atoi(argv[1]));

  if (bind(serv_sock, (struct sockaddr *)&serv_adr, sizeof(serv_adr)) == -1)
  {
    error_handling("bind() error");
  }
  if (listen(serv_sock, 5) == -1)
  {
    error_handling("listen() error");
  }

  while (1)
  {
    clnt_adr_sz = sizeof(clnt_adr);
    clnt_sock = accept(serv_sock, (struct sockaddr *)&clnt_adr, &clnt_adr_sz);

    pthread_mutex_lock(&mutx);
    clnt_socks[clnt_cnt++] = clnt_sock;
    pthread_mutex_unlock(&mutx);

    pthread_create(&t_id, NULL, handle_clnt, (void *)&clnt_sock);
    pthread_detach(t_id);
    printf("Connected client IP: %s \n", inet_ntoa(clnt_adr.sin_addr));
  }

  close(serv_sock);
  return 0;
}
