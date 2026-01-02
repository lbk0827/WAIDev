namespace WaiJigsaw.Data
{
	public enum AD_FORMAT
	{
		REWARDEDVIDEO = 0,								// 광고종류 | 보상형 광고
		INTERSTITIAL = 1,								// 광고종류 | 전면 광고
		BANNER = 2,										// 광고종류 | 배너 광고
	}

	public enum ADS_TYPE
	{
		DEFAULT_REWARDEDVIDEO = 0,						// 광고타입 | 기본 보상형광고
		DEFAULT_INTERSTITIAL = 1,						// 광고타입 | 기본 전면광고
		DEFAULT_BANNER = 2,								// 광고타입 | 기본 배너광고
		ATTENDANCE_MOREREWARDS = 3,						// 광고타입 | 출석 추가 보상
		INGAME_STAGECLEAR = 4,							// 광고타입 | 인게임 스테이지 클리어 Segment 1
		PLAYTIME = 5,									// 광고타입 | 체류 시간 Segment 1
		LOBBY_HEART = 6,								// 광고타입 | 로비 하트 광고
		INGAME_EXTRAREWARD = 7,							// 광고타입 | 인게임 추가보상
		STORE_GETFREECOIN = 8,							// 광고타입 | 상점 코인 무료 보상
		INGAME_STAGECLEAR_2 = 9,						// 광고타입 | 인게임 스테이지 클리어 Segment 2
		PLAYTIME_2 = 10,								// 광고타입 | 체류 시간 Segment 2
	}

	public enum CONDITIONCHECKER_TYPE
	{
		CANPLAYINTERSTITIALAD = 0,						// 컨디션 체커 | 전면 광고 조건 체크
		CLEARSTAGE = 1,									// 컨디션 체커 | 스테이지 클리어 체크
		GETATTENDANCEREWARDDAY = 2,						// 컨디션 체커 | 출석 보상 획득 체크
		ISCLEARSTAGE = 3,								// 컨디션 체커 | 스테이지 클리어 했는지
		ENTERSTAGE = 4,									// 컨디션 체커 | 스테이지 입장
	}

	public enum MULTIPLECONDITION_TYPE
	{
		AND = 0,										// 복합 컨디션 | AND
		OR = 1,											// 복합 컨디션 | OR
	}

	public enum ADS_GROUP_TYPE
	{
		NONE = 0,										// 광고 그룹 타입 | None
		INGAME_STAGECLEAR_OR_PLAYTIME = 1,				// 광고 그룹 타입 | 스테이지 클리어 OR 플레이시간 Segment 1
		INGAME_STAGECLEAR_AND_PLAYTIME = 2,				// 광고 그룹 타입 | 스테이지 클리어 AND 플레이 시간 Segment 1
		INGAME_STAGECLEAR_OR_PLAYTIME_2 = 3,			// 광고 그룹 타입 | 스테이지 클리어 OR 플레이시간 Segment 2
		INGAME_STAGECLEAR_AND_PLAYTIME_2 = 4,			// 광고 그룹 타입 | 스테이지 클리어 AND 플레이 시간 Segment 2
	}

	public enum ERROR_LEVEL
	{
		WARN = 0,										// 에러 레벨 | 경고
		ERROR = 1,										// 에러 레벨 | 에러
		FATAL = 3,										// 에러 레벨 | 치명적 에러
	}

	public enum ERROR_ACTION
	{
		NONE = 0,										// 에러 발생 시 대응 | 없음
		NOTICE = 1,										// 에러 발생 시 대응 | 경고 메시지 출력
		LOGOUT = 2,										// 에러 발생 시 대응 | 로그 아웃
		TOAST = 3,										// 에러 발생 시 대응 | 토스트 메시지 출력
		SYNC = 4,										// 에러 발생 시 대응 | sync 요청
		VERSION_CHECK = 5,								// 에러 발생 시 대응 | 버전 에러, 마켓 업데이트로 이동
		VERSION_CHECK_OPTIONAL = 6,						// 에러 발생 시 대응 | 버전 에러, 마켓 업데이트로 이동(옵셔널)
		DEL_CACHE = 7,									// 에러 발생 시 대응 | 클라이언트 캐쉬 삭제
	}

	public enum ITEM_TYPE
	{
		NONE = 0,										// #REF!
		COIN = 101,										// #REF!
	}

	public enum ITEM_GETTYPE
	{
		NONE = 0,										// 아이템 획득 방법 | None
		DEFAULT = 1,									// 아이템 획득 방법 | Default(초기 사용자 계정에 지급된 상태)
		PRICE = 2,										// 아이템 획득 방법 | Price(Item_Price 지급)
		ADSFREE = 3,									// 아이템 획득 방법 | AdsFree
	}

	public enum ITEM_CATEGORY
	{
		NONE = 0,										// 아이템 카테고리 | None
		CURRENCY = 1,									// 아이템 카테고리 | 재화
		CONSUMABLE = 2,									// 아이템 카테고리 | 소비성 아이템
		BUFF = 3,										// 아이템 카테고리 | 버프
		CONTINUE = 4,									// 아이템 카테고리 | 이어 하기 아이템
		STARTINGBOOSTER = 5,							// 아이템 카테고리 | 스타팅 부스터
	}

	public enum HAPTICTYPE
	{
		CONSTANT = 0,									// 진동 타입 | Constant
		PRESET_SELECTION = 1,							// 진동 타입 | Preset_Selection
		PRESET_SUCCESS = 2,								// 진동 타입 | Preset_Success
		PRESET_WARNING = 3,								// 진동 타입 | Preset_Warning
		PRESET_FAILURE = 4,								// 진동 타입 | Preset_Failure
		PRESET_LIGHTIMPACT = 5,							// 진동 타입 | Preset_LightImpact
		PRESET_MEDIUMIMPACT = 6,						// 진동 타입 | Preset_MediumImpact
		PRESET_HEAVYIMPACT = 7,							// 진동 타입 | Preset_HeavyImpact
		PRESET_RIGIDIMPACT = 8,							// 진동 타입 | Preset_RigidImpact
		PRESET_SOFTIMPACT = 9,							// 진동 타입 | Preset_SoftImpact
		HAPTIC_CLIP = 10,								// 진동 타입 | Haptic_Clip
	}

	public enum LOCALPUSH_TYPE
	{
		ATTENDANCE_NEXT_DAY = 0,						// 푸시 타입 | 출석부 다음날
		HEART_MAX = 1,									// 푸시 타입 | 하트 맥스
		DAILY_WAKEUP = 2,								// 푸시 타입 | 데일리 푸시2
		PICKONE_END = 3,								// 푸시 타입 | 픽원 종료 알림
		VERTICAL_END = 4,								// 푸시 타입 | 엔드리스(세로형) 종료 알림
		CHAIN_END = 5,									// 푸시 타입 | 엔드리스(체인형) 종료 알림
	}

	public enum PURCHASE_TYPE
	{
		NONE = 0,										// 상품 구매 형태 | None
		CASH = 1,										// 상품 구매 형태 | 캐쉬
		ADSFREE = 2,									// 상품 구매 형태 | 광고 시청 후 제공
		FREE = 3,										// 상품 구매 형태 | 무료 제공
	}

	public enum BUNDLE_TYPE
	{
		NONE = 0,										// 패키지 타입 | None
		NOADS = 1,										// 패키지 타입 | 광고 제거 타입
		COMMON = 2,										// 패키지 타입 | 일반 타입
		INGAMESHOP = 3,									// 패키지 타입 | 인게임 상점 타입
		SPECIALOFFER = 4,								// 패키지 타입 | 스페셜 오퍼 타입
	}

	public enum PACKAGE_BG_TYPE
	{
		NONE = 0,										// 패키지 배경 | None
		NOADS = 1,										// 패키지 배경 | 광고 제거 포함
		COMMON_1 = 2,									// 패키지 배경 | 일반 패키지 1
		COMMON_2 = 3,									// 패키지 배경 | 일반 패키지 2
		COMMON_3 = 4,									// 패키지 배경 | 일반 패키지 3
		SB_1 = 5,										// 패키지 배경 | 버닝 번들 1
		COMMON_4 = 6,									// 패키지 배경 | 실패 번들
	}

	public enum SPECIAL_OFFER_TYPE
	{
		NONE = 0,										// 스페셜 오퍼 타입 | None
		PICKONE = 1,									// 스페셜 오퍼 타입 | 픽원 오퍼
		WELCOMEDEAL = 2,								// 스페셜 오퍼 타입 | 웰컴딜
		ENDLESSOFFER = 3,								// 스페셜 오퍼 타입 | 엔드리스 오퍼
	}

	public enum PICKONE_OFFER_TYPE
	{
		NONE = 0,										// 픽원 오퍼 타입 | None
		PICKONE_1 = 1,									// 픽원 오퍼 타입 | 시즌1
		PICKONE_2 = 2,									// 픽원 오퍼 타입 | 시즌2
		PICKONE_3 = 3,									// 픽원 오퍼 타입 | 시즌3
		PICKONE_4 = 4,									// 픽원 오퍼 타입 | 시즌4
		PICKONE_5 = 5,									// 픽원 오퍼 타입 | 시즌5
		PICKONE_6 = 6,									// 픽원 오퍼 타입 | 시즌6
		PICKONE_7 = 7,									// 픽원 오퍼 타입 | 시즌7
		PICKONE_8 = 8,									// 픽원 오퍼 타입 | 시즌8
		PICKONE_9 = 9,									// 픽원 오퍼 타입 | 시즌9
	}

	public enum WELCOME_DEAL_TYPE
	{
		NONE = 0,										// 웰컴딜 타입 | None
		WELCOMEDEAL_1 = 1,								// 웰컴딜 타입 | 시즌1
		WELCOMEDEAL_2 = 2,								// 웰컴딜 타입 | 시즌2
	}

	public enum ENDLESS_OFFER_TYPE
	{
		NONE = 0,										// 엔드리스 오퍼 | None
		ENDLESSOFFER_1 = 1,								// 엔드리스 오퍼 | 세로형 1
		ENDLESSGIFT_1 = 2,								// 엔드리스 오퍼 | 체인형 1
	}

	public enum EVENT_TYPE
	{
		BLOCKPASS = 0,									// 이벤트 타입 | 블록 패스
		FACTORYCHASE = 1,								// 이벤트 타입 | 팩토리 채스
		ADVENTURESTREAK = 2,							// 이벤트 타입 | 어드벤어스트릭
		PINATAPARTY = 3,								// 이벤트 타입 | 피냐타 파티
	}
}