// ���� ifdef ���Ǵ���ʹ�� DLL �������򵥵�
// ��ı�׼�������� DLL �е������ļ��������������϶���� SECURITYACCESS_EXPORTS
// ���ű���ġ���ʹ�ô� DLL ��
// �κ�������Ŀ�ϲ�Ӧ����˷��š�������Դ�ļ��а������ļ����κ�������Ŀ���Ὣ
// SECURITYACCESS_API ������Ϊ�Ǵ� DLL ����ģ����� DLL ���ô˺궨���
// ������Ϊ�Ǳ������ġ�
#ifdef SECURITYACCESS_EXPORTS
#define SECURITYACCESS_API __declspec(dllexport)
#else
#define SECURITYACCESS_API __declspec(dllimport)
#endif

EXTERN_C SECURITYACCESS_API UINT32 __cdecl SecurityAccess(UINT32 project, UINT32 seed, UINT32 level);


