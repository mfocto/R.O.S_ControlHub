-- room
CREATE TABLE rooms (
                       room_pk     BIGSERIAL PRIMARY KEY,
                       room_id     TEXT NOT NULL UNIQUE,
                       name        TEXT,
                       created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
                       updated_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE rooms IS '공정/셀/라인 등 설비들이 속한 공간';
COMMENT ON COLUMN rooms.room_pk IS 'DB 내부 식별자';
COMMENT ON COLUMN rooms.room_id IS 'room ID';
COMMENT ON COLUMN rooms.name IS '화면 표시용 이름';
COMMENT ON COLUMN rooms.created_at IS '생성 시각';
COMMENT ON COLUMN rooms.updated_at IS '마지막 수정 시각';

CREATE INDEX idx_rooms_room_id ON rooms(room_id);

-- devices 
CREATE TABLE devices (
                         device_pk    BIGSERIAL PRIMARY KEY,
                         room_pk      BIGINT NOT NULL REFERENCES rooms(room_pk) ON DELETE CASCADE,

                         device_id    TEXT NOT NULL,
                         device_type  TEXT NOT NULL,
                         device_meta  JSONB NOT NULL DEFAULT '{}'::jsonb,

                         created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
                         updated_at   TIMESTAMPTZ NOT NULL DEFAULT now(),

                         UNIQUE (room_pk, device_id)
);

COMMENT ON TABLE devices IS '실제 제어 대상 장비';
COMMENT ON COLUMN devices.device_pk IS '식별자';
COMMENT ON COLUMN devices.room_pk IS '장비가 속한 공간(room)';
COMMENT ON COLUMN devices.device_id IS '장비 ID (예: robot-1)';
COMMENT ON COLUMN devices.device_type IS '장비 종류 (robot, conveyor, agv)';
COMMENT ON COLUMN devices.device_meta IS '장비 메타정보 (모델, 축 수, IP 등)';
COMMENT ON COLUMN devices.created_at IS '생성 시각';
COMMENT ON COLUMN devices.updated_at IS '마지막 수정 시각';

CREATE INDEX idx_devices_room ON devices(room_pk);
CREATE INDEX idx_devices_type ON devices(device_type);
CREATE INDEX idx_devices_device_id ON devices(device_id);

-- control_state_current
CREATE TABLE control_state_current (
                                       device_pk        BIGINT PRIMARY KEY REFERENCES devices(device_pk) ON DELETE CASCADE,

                                       version          BIGINT NOT NULL DEFAULT 0,
                                       state_json       JSONB  NOT NULL DEFAULT '{}'::jsonb,

                                       last_command_id  UUID,
                                       last_actor_type  TEXT NOT NULL DEFAULT 'system',
                                       last_actor_id    TEXT,

                                       updated_at       TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE control_state_current IS '장비별 목표 상태(서버 기준)';
COMMENT ON COLUMN control_state_current.device_pk IS '대상 장비';
COMMENT ON COLUMN control_state_current.version IS '상태 변경 버전(중복처리방지)';
COMMENT ON COLUMN control_state_current.state_json IS '목표 상태 JSON';
COMMENT ON COLUMN control_state_current.last_command_id IS '이 상태를 만든 명령 ID';
COMMENT ON COLUMN control_state_current.last_actor_type IS '상태 변경 주체';
COMMENT ON COLUMN control_state_current.last_actor_id IS '상태 변경 주체 식별자';
COMMENT ON COLUMN control_state_current.updated_at IS '마지막 변경 시각';

CREATE INDEX idx_control_state_current_updated
    ON control_state_current(updated_at);

CREATE INDEX idx_control_state_current_state_json
    ON control_state_current USING GIN (state_json);

-- control_state_events
CREATE TABLE control_state_events (
                                      event_pk     BIGSERIAL PRIMARY KEY,
                                      device_pk    BIGINT NOT NULL REFERENCES devices(device_pk) ON DELETE CASCADE,

                                      command_id   UUID NOT NULL DEFAULT gen_random_uuid(),
                                      prev_version BIGINT,
                                      next_version BIGINT NOT NULL,

                                      patch_json   JSONB NOT NULL,
                                      state_json   JSONB,

                                      actor_type   TEXT NOT NULL,
                                      actor_id     TEXT,

                                      created_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE control_state_events IS '상태 변경 히스토리 (명령 단위 추적)';
COMMENT ON COLUMN control_state_events.command_id IS '명령 ID (중복 방지/추적)';
COMMENT ON COLUMN control_state_events.prev_version IS '변경 전 버전';
COMMENT ON COLUMN control_state_events.next_version IS '변경 후 버전';
COMMENT ON COLUMN control_state_events.patch_json IS '변경된 항목만 담은 JSON';
COMMENT ON COLUMN control_state_events.state_json IS '변경 후 전체 상태 스냅샷';
COMMENT ON COLUMN control_state_events.actor_type IS '명령 주체';
COMMENT ON COLUMN control_state_events.created_at IS '이벤트 발생 시각';

CREATE UNIQUE INDEX uq_events_device_command
    ON control_state_events(device_pk, command_id);

CREATE INDEX idx_events_device_time
    ON control_state_events(device_pk, created_at DESC);

CREATE INDEX idx_events_created_at
    ON control_state_events(created_at DESC);

CREATE INDEX idx_events_patch_json
    ON control_state_events USING GIN (patch_json);


-- control_apply_status
CREATE TABLE control_apply_status (
                                      apply_pk             BIGSERIAL PRIMARY KEY,
                                      device_pk            BIGINT NOT NULL REFERENCES devices(device_pk) ON DELETE CASCADE,

                                      target_system        TEXT NOT NULL,
                                      status               TEXT NOT NULL DEFAULT 'pending',

                                      target_version       BIGINT NOT NULL,
                                      last_applied_version BIGINT NOT NULL DEFAULT 0,

                                      retry_count          INT NOT NULL DEFAULT 0,
                                      last_attempt_at      TIMESTAMPTZ,
                                      last_error           TEXT,

                                      updated_at           TIMESTAMPTZ NOT NULL DEFAULT now(),

                                      UNIQUE (device_pk, target_system)
);

COMMENT ON TABLE control_apply_status IS 'ROS/OPC UA 등 외부 시스템 적용 상태 추적';
COMMENT ON COLUMN control_apply_status.target_system IS '적용 대상 시스템';
COMMENT ON COLUMN control_apply_status.status IS '적용 상태 (pending, applied, failed)';
COMMENT ON COLUMN control_apply_status.target_version IS '적용 목표 버전';
COMMENT ON COLUMN control_apply_status.last_applied_version IS '실제 적용 완료된 버전';
COMMENT ON COLUMN control_apply_status.last_error IS '실패 시 에러 메시지';

CREATE INDEX idx_apply_status_device
    ON control_apply_status(device_pk);

CREATE INDEX idx_apply_status_status
    ON control_apply_status(status);

CREATE INDEX idx_apply_status_updated
    ON control_apply_status(updated_at DESC);


-- device_actual_state_history
CREATE TABLE device_actual_state_history (
                                             actual_pk   BIGSERIAL PRIMARY KEY,
                                             device_pk   BIGINT NOT NULL REFERENCES devices(device_pk) ON DELETE CASCADE,

                                             reported_at TIMESTAMPTZ NOT NULL,
                                             state_json  JSONB NOT NULL,

                                             created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE device_actual_state_history IS '장비의 실제 위치/상태 이력';
COMMENT ON COLUMN device_actual_state_history.reported_at IS '장비가 상태를 보고한 시각';
COMMENT ON COLUMN device_actual_state_history.state_json IS '실제 상태 JSON';

CREATE INDEX idx_actual_device_time
    ON device_actual_state_history(device_pk, reported_at DESC);

CREATE INDEX idx_actual_reported_at
    ON device_actual_state_history(reported_at DESC);

CREATE INDEX idx_actual_state_json
    ON device_actual_state_history USING GIN (state_json);


-- system_logs
CREATE TABLE system_logs (
                             log_pk       BIGSERIAL PRIMARY KEY,
                             room_pk      BIGINT REFERENCES rooms(room_pk),
                             device_pk    BIGINT REFERENCES devices(device_pk),

                             component    TEXT NOT NULL,
                             severity     TEXT NOT NULL,
                             event_type   TEXT NOT NULL,

                             command_id   UUID,
                             message      TEXT,
                             payload_json JSONB,

                             occurred_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE system_logs IS '웹/서버/유니티/브리지 전체 로그 및 오류 기록';
COMMENT ON COLUMN system_logs.component IS '로그 발생 컴포넌트';
COMMENT ON COLUMN system_logs.severity IS '로그 수준 (info/warn/error)';
COMMENT ON COLUMN system_logs.event_type IS '로그 이벤트 유형';
COMMENT ON COLUMN system_logs.command_id IS '연관 명령 ID';
COMMENT ON COLUMN system_logs.payload_json IS '로그 상세 정보';

CREATE INDEX idx_logs_device_time
    ON system_logs(device_pk, occurred_at DESC);

CREATE INDEX idx_logs_room_time
    ON system_logs(room_pk, occurred_at DESC);

CREATE INDEX idx_logs_command
    ON system_logs(command_id);

CREATE INDEX idx_logs_severity
    ON system_logs(severity);

CREATE INDEX idx_logs_payload
    ON system_logs USING GIN (payload_json);