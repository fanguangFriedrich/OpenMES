<template>
  <div class="detail-container" v-loading="loading">
    <!-- 顶部返回栏 -->
    <div class="detail-header">
      <el-button size="small" icon="el-icon-arrow-left" @click="goBack">返回列表</el-button>
      <span class="detail-title">派工单详情</span>
    </div>

    <template v-if="detail">
      <!-- 基本信息卡片 -->
      <div class="detail-card">
        <div class="card-title">基本信息</div>
        <el-descriptions :column="4" border size="small">
          <el-descriptions-item label="打开时间">
            {{ formatTime(detail.mesWorkOrderDto.createdTime) }}
          </el-descriptions-item>
          <el-descriptions-item label="派工人">
            {{ detail.mesWorkOrderDto.dispatchWorkers || '--' }}
          </el-descriptions-item>
          <el-descriptions-item label="派工单号">
            {{ detail.mesWorkOrderDto.workOrder || '--' }}
          </el-descriptions-item>
          <el-descriptions-item label="承诺到料日期">
            {{ formatDate(detail.mesWorkOrderDto.promiseDeliveryDate) }}
          </el-descriptions-item>
          <el-descriptions-item label="计划开始时间">
            {{ formatDate(detail.mesWorkOrderDto.startDate) }}
          </el-descriptions-item>
          <el-descriptions-item label="计划结束时间">
            {{ formatDate(detail.mesWorkOrderDto.endDate) }}
          </el-descriptions-item>
          <el-descriptions-item label="签收码">
            <el-image
              style="width: 96px; height: 96px"
              :src="qrCodeUrl"
              fit="contain"
            >
              <div slot="error" class="image-slot">
                <i class="el-icon-picture-outline"></i>
              </div>
            </el-image>
          </el-descriptions-item>
          <el-descriptions-item label="是否加急">
            {{ detail.mesWorkOrderDto.status === '1' ? '是' : '--' }}
          </el-descriptions-item>
        </el-descriptions>
      </div>

      <!-- 工序信息卡片 -->
      <div class="detail-card" style="margin-top: 16px;">
        <div class="card-title">工序信息</div>
        <el-table :data="detail.mesWorkOrderProcessDtos" border size="small" style="width: 100%">
          <el-table-column prop="processItemNo" label="工序" width="80" />
          <el-table-column prop="processText" label="工艺" width="140" />
          <el-table-column prop="workCenterName" label="工作中心" width="160" />
          <el-table-column prop="departmentName" label="组别" width="180" />
          <el-table-column prop="productionValue" label="产值" width="100" />
          <el-table-column label="条码" width="120">
            <template slot-scope="scope">
              {{ scope.row.workOrderCode || '--' }}
            </template>
          </el-table-column>
          <el-table-column label="计划开始时" width="160">
            <template slot-scope="scope">
              {{ formatTime(scope.row.sapCreatedTime) }}
            </template>
          </el-table-column>
        </el-table>
      </div>

      <!-- 生产订单卡片 -->
      <div class="detail-card" style="margin-top: 16px;">
        <div class="card-title">生产订单</div>
        <el-table :data="detail.mesPoDtos" border size="small" style="width: 100%">
          <el-table-column prop="productionOrderNo" label="生产订单" width="160" />
          <el-table-column prop="drawNo" label="图纸编号" width="200" />
          <el-table-column prop="materialDescription" label="图纸名称" min-width="200" show-overflow-tooltip />
          <el-table-column prop="drawRevision" label="图纸版本" width="100" />
          <el-table-column prop="quantity" label="数量" width="80" />
          <el-table-column prop="materialNo" label="产品条码" width="160" />
          <el-table-column label="操作" width="100" fixed="right">
            <template slot-scope="scope">
              <el-button type="text" size="small" @click="viewPoDetail(scope.row)">查看</el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </template>
  </div>
</template>

<script>
import { checkMesWorkOrder, getMesWorkOrderDetail } from '@/api/mesWorkOrders'

export default {
  name: 'mesWorkOrderDetail',

  data() {
    return {
      workOrder: this.$route.query.workOrder || '',
      loading: false,
      detail: null,
      qrCodeUrl: ''
    }
  },

  mounted() {
    if (this.workOrder) {
      this.loadDetail()
    } else {
      this.$message.error('派工单号不能为空')
    }
  },

  methods: {
    async loadDetail() {
      this.loading = true
      try {
        // 第一步：校验派工单
        const checkRes = await checkMesWorkOrder(this.workOrder)
        if (!checkRes.data) {
          this.$message.error(checkRes.message || '派工单不存在')
          return
        }
        // 第二步：获取详情
        const detailRes = await getMesWorkOrderDetail(this.workOrder)
        if (detailRes.code === 200) {
          this.detail = detailRes.data
        } else {
          this.$message.error(detailRes.message || '获取详情失败')
        }
      } catch (e) {
        this.$message.error('请求失败：' + e.message)
      } finally {
        this.loading = false
      }
    },

    formatDate(timestamp) {
      if (!timestamp) return '--'
      const d = new Date(timestamp)
      return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
    },

    formatTime(timestamp) {
      if (!timestamp) return '--'
      const d = new Date(timestamp)
      return `${this.formatDate(timestamp)} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
    },

    viewPoDetail(row) {
      this.$router.push({ path: '/mes/po-detail', query: { productionOrderNo: row.productionOrderNo } })
    },

    goBack() {
      this.$router.back()
    }
  }
}
</script>

<style scoped>
.detail-container {
  padding: 20px;
  background: #f0f2f5;
  min-height: 100%;
}

.detail-header {
  display: flex;
  align-items: center;
  gap: 16px;
  background: #fff;
  padding: 14px 20px;
  border-radius: 4px;
  margin-bottom: 16px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
}

.detail-title {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}

.detail-card {
  background: #fff;
  border-radius: 4px;
  padding: 20px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
}

.card-title {
  font-size: 14px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 16px;
  padding-left: 10px;
  border-left: 3px solid #409eff;
}

.image-slot {
  display: flex;
  justify-content: center;
  align-items: center;
  width: 100%;
  height: 100%;
  background: #f5f7fa;
  color: #909399;
  font-size: 28px;
}
</style>