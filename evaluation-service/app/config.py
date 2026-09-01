from functools import lru_cache

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """
    Cau hinh Evaluation Service. Toan bo gia tri doc tu bien moi truong / .env,
    khong hard-code secret (dung nguyen tac #8 prompt goc).
    """

    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", extra="ignore")

    # URL goc cua Toeic.Api backend (.NET), vi du https://localhost:5443
    backend_base_url: str

    # Tai khoan RATER dung rieng cho Evaluation Service de goi API so sanh AI vs Human.
    # Khuyen nghi tao 1 tai khoan RATER dedicated (qua /auth/register-staff) thay vi dung
    # tai khoan cua nguoi that, de tach bach quyen truy cap va de audit log.
    eval_service_username: str
    eval_service_password: str

    # Backend dev dung self-signed cert (xem README.md goc dung curl -k) -> mac dinh false
    # o dev, PHAI true khi tro vao backend production co cert hop le.
    backend_verify_ssl: bool = False

    # So dong lay moi lan goi trang (page) khi phan trang qua endpoint comparison.
    backend_page_size: int = 100

    # Timeout (giay) cho moi HTTP call toi backend.
    backend_request_timeout_seconds: float = 30.0


@lru_cache
def get_settings() -> Settings:
    return Settings()
