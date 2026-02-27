#!/bin/bash

# 插件系统 API 测试脚本
# 使用方法: ./scripts/test-api.sh [base_url]

BASE_URL="${1:-http://localhost:8081}"
PASSED=0
FAILED=0

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "=========================================="
echo "  插件系统 API 测试"
echo "  Base URL: $BASE_URL"
echo "=========================================="
echo ""

# 测试函数
test_endpoint() {
    local name="$1"
    local method="$2"
    local endpoint="$3"
    local data="$4"
    local expected_status="$5"
    local expected_content="$6"

    echo -n "测试: $name ... "

    if [ "$method" == "GET" ]; then
        response=$(curl -s -w "\n%{http_code}" "$BASE_URL$endpoint")
    else
        response=$(curl -s -w "\n%{http_code}" -X "$method" "$BASE_URL$endpoint" \
            -H "Content-Type: application/json" \
            -d "$data")
    fi

    status_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')

    if [ "$status_code" == "$expected_status" ]; then
        if [ -n "$expected_content" ]; then
            if echo "$body" | grep -q "$expected_content"; then
                echo -e "${GREEN}✓ PASSED${NC}"
                ((PASSED++))
            else
                echo -e "${RED}✗ FAILED${NC} (内容不匹配)"
                echo "  期望包含: $expected_content"
                echo "  实际响应: $body"
                ((FAILED++))
            fi
        else
            echo -e "${GREEN}✓ PASSED${NC}"
            ((PASSED++))
        fi
    else
        echo -e "${RED}✗ FAILED${NC} (状态码: $status_code, 期望: $expected_status)"
        echo "  响应: $body"
        ((FAILED++))
    fi
}

echo "--- 健康检查 ---"
test_endpoint "健康检查" "GET" "/health" "" "200" "Healthy"

echo ""
echo "--- 插件管理 API ---"
test_endpoint "获取插件列表" "GET" "/api/plugins" "" "200" "PluginA"
test_endpoint "获取插件状态" "GET" "/api/plugins/status" "" "200" "loadedPluginCount"
test_endpoint "获取 PluginA 详情" "GET" "/api/plugins/PluginA" "" "200" "计算器插件"
test_endpoint "获取 PluginB 详情" "GET" "/api/plugins/PluginB" "" "200" "问候插件"
test_endpoint "获取不存在的插件" "GET" "/api/plugins/NotExist" "" "404" ""

echo ""
echo "--- 计算器插件 API ---"
test_endpoint "加法 10+5=15" "POST" "/api/calculator/add" '{"a":10,"b":5}' "200" '"result":15'
test_endpoint "减法 20-8=12" "POST" "/api/calculator/subtract" '{"a":20,"b":8}' "200" '"result":12'
test_endpoint "乘法 6*7=42" "POST" "/api/calculator/multiply" '{"a":6,"b":7}' "200" '"result":42'
test_endpoint "除法 100/4=25" "POST" "/api/calculator/divide" '{"a":100,"b":4}' "200" '"result":25'
test_endpoint "除以零" "POST" "/api/calculator/divide" '{"a":10,"b":0}' "400" ""
test_endpoint "幂运算 2^10=1024" "POST" "/api/calculator/power" '{"a":2,"b":10}' "200" '"result":1024'
test_endpoint "平方根 √81=9" "POST" "/api/calculator/sqrt" '{"value":81}' "200" '"result":9'
test_endpoint "负数平方根" "POST" "/api/calculator/sqrt" '{"value":-1}' "400" ""
test_endpoint "计算历史" "GET" "/api/calculator/history" "" "200" ""

echo ""
echo "--- 问候插件 API ---"
test_endpoint "默认问候" "GET" "/api/greeting/World" "" "200" "Hello, World!"
test_endpoint "中文问候" "GET" "/api/greeting/张三?language=zh" "" "200" "你好，张三"
test_endpoint "日语问候" "GET" "/api/greeting/田中?language=ja" "" "200" "こんにちは"
test_endpoint "韩语问候" "GET" "/api/greeting/김철수?language=ko" "" "200" "안녕하세요"
test_endpoint "西班牙语问候" "GET" "/api/greeting/Maria?language=es" "" "200" "Hola"
test_endpoint "法语问候" "GET" "/api/greeting/Pierre?language=fr" "" "200" "Bonjour"
test_endpoint "德语问候" "GET" "/api/greeting/Hans?language=de" "" "200" "Hallo"
test_endpoint "获取支持的语言" "GET" "/api/greeting/languages" "" "200" '"code":"en"'

echo ""
echo "=========================================="
echo "  测试结果"
echo "=========================================="
echo -e "  ${GREEN}通过: $PASSED${NC}"
echo -e "  ${RED}失败: $FAILED${NC}"
echo "  总计: $((PASSED + FAILED))"
echo "=========================================="

if [ $FAILED -gt 0 ]; then
    exit 1
fi
